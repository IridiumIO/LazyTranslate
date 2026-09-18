Imports System.IO
Imports System.Globalization
Imports System.Net.Http
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks

Imports NuGet.Versioning

Imports Tomlyn
Imports Tomlyn.Model

Public Module Localisation

    Friend Property ApplicationName As String
    Friend Property ProgramVersion As String

    Friend Property OutputMissingTranslationsToDebug As Boolean = False

    Private ReadOnly HttpClient As New HttpClient()

    Public Async Function InitializeAsync(options As LocalisationOptions) As Task
        ApplicationName = options.ApplicationName
        ProgramVersion = If(options.ProgramVersion, GetProgramVersion(options.ResourceAssembly))
        L.CatalogueDirectory = options.CatalogueDirectory
        OutputMissingTranslationsToDebug = options.OutputMissingTranslationsToDebug
        Directory.CreateDirectory(options.CatalogueDirectory)
        Await SynchroniseEmbeddedLanguagesAsync(options, options.ApplicationName & ".")

        Dim languageCode = If(options.InitialCulture, options.SourceCulture)
        If Not L.TryLoadLanguage(languageCode) Then L.UseEnglish(options.SourceCulture)
    End Function

    Public Function InitialiseAsync(options As LocalisationOptions) As Task
        Return InitializeAsync(options)
    End Function

    Public Function GetAvailableLanguages() As List(Of LanguageItem)
        Return Directory.EnumerateFiles(L.CatalogueDirectory, "*.toml").
            Where(Function(f) Not String.Equals(Path.GetFileNameWithoutExtension(f), "manifest", StringComparison.OrdinalIgnoreCase)).
            Select(Function(file) New LanguageItem(Path.GetFileNameWithoutExtension(file))).
            OrderBy(Function(language) language.NativeName).
            ToList()
    End Function

    Public Async Function CheckForLanguageUpdateAsync(cultureName As String, catalogueBaseUri As Uri) As Task(Of Boolean)
        Try
            Dim localPath = Path.Combine(L.CatalogueDirectory, cultureName & ".toml")
            Dim manifestText = Await HttpClient.GetStringAsync(New Uri(catalogueBaseUri, "manifest.toml"))
            Dim manifest = TomlSerializer.Deserialize(Of TomlTable)(manifestText)
            Dim locales = TryCast(manifest("locales"), TomlTable)

            If locales Is Nothing OrElse Not locales.ContainsKey(cultureName) Then Return False

            Dim locale = TryCast(locales(cultureName), TomlTable)
            If locale Is Nothing Then Return False

            Dim remoteVersion = Convert.ToInt32(locale("version"))
            Dim remoteFileName = CStr(locale("file"))
            Dim localVersion = If(File.Exists(localPath),
                                  ReadLocalisationVersion(Await File.ReadAllTextAsync(localPath)),
                                  0)

            If remoteVersion <= localVersion Then Return False

            Dim remoteText = Await HttpClient.GetStringAsync(New Uri(catalogueBaseUri, remoteFileName))
            If ReadLocalisationVersion(remoteText) <> remoteVersion Then Return False
            If Not ProgramVersionsEqual(ReadProgramVersion(remoteText), ProgramVersion) Then Return False
            TomlSerializer.Deserialize(Of TomlTable)(remoteText)

            Dim tempPath = localPath & ".tmp"
            Await File.WriteAllTextAsync(tempPath, remoteText, Encoding.UTF8)
            File.Move(tempPath, localPath, True)

            Return True
        Catch ex As Exception
            Debug.WriteLine($"[LOC] Failed to check for localisation update: {ex.Message}")
            Return False
        End Try
    End Function

    Private Async Function SynchroniseEmbeddedLanguagesAsync(options As LocalisationOptions, resourcePrefix As String) As Task
        For Each resourceName In options.ResourceAssembly.GetManifestResourceNames()
            If Not resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal) OrElse
               Not resourceName.EndsWith(".toml", StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim name = Path.GetFileNameWithoutExtension(resourceName.Substring(resourcePrefix.Length))
            Dim languageCode = name.Substring(name.LastIndexOf("."c) + 1)
            If languageCode.Equals("manifest", StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim outputPath = Path.Combine(options.CatalogueDirectory, languageCode & ".toml")
            Using stream = options.ResourceAssembly.GetManifestResourceStream(resourceName)
                Using reader As New StreamReader(stream, Encoding.UTF8)
                    Dim embeddedText = Await reader.ReadToEndAsync()
                    Dim localText = If(File.Exists(outputPath), Await File.ReadAllTextAsync(outputPath), Nothing)
                    Dim localProgramVersion = ReadProgramVersion(localText)

                    If localText Is Nothing OrElse
                       IsOlderProgramVersion(localProgramVersion, ProgramVersion) OrElse
                       ProgramVersionsEqual(localProgramVersion, ProgramVersion) AndAlso
                       ReadLocalisationVersion(embeddedText) > ReadLocalisationVersion(localText) Then
                        Await File.WriteAllTextAsync(outputPath, embeddedText, Encoding.UTF8)
                    End If
                End Using
            End Using
        Next
    End Function

    Public Function ReadLocalisationVersion(sourceText As String) As Integer
        Dim version As Integer
        Integer.TryParse(ReadHeaderValue(sourceText, "# @version "), version)
        Return version
    End Function

    Public Function ReadProgramVersion(sourceText As String) As String
        Return ReadHeaderValue(sourceText, "# @programversion ")
    End Function

    Private Function ReadHeaderValue(sourceText As String, prefix As String) As String
        If sourceText Is Nothing Then Return Nothing

        Using reader As New StringReader(sourceText)
            While True
                Dim line = reader.ReadLine()
                If line Is Nothing Then Exit While
                line = line.Trim()

                If line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then Return line.Substring(prefix.Length).Trim()

                If Not String.IsNullOrWhiteSpace(line) AndAlso Not line.StartsWith("#"c) Then Exit While
            End While
        End Using
        Return Nothing
    End Function

    Private Function GetProgramVersion(resourceAssembly As Assembly) As String
        Dim informationalVersion = resourceAssembly.GetCustomAttribute(Of AssemblyInformationalVersionAttribute)()?.InformationalVersion
        Return informationalVersion?.Split("+"c)(0)
    End Function

    Private Function ProgramVersionsEqual(left As String, right As String) As Boolean
        Dim leftVersion As NuGetVersion = Nothing
        Dim rightVersion As NuGetVersion = Nothing

        If NuGetVersion.TryParse(left, leftVersion) AndAlso NuGetVersion.TryParse(right, rightVersion) Then
            Return VersionComparer.VersionRelease.Equals(leftVersion, rightVersion)
        End If

        Return String.Equals(left, right, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsOlderProgramVersion(existingVersion As String, currentVersion As String) As Boolean
        Dim existing As NuGetVersion = Nothing
        Dim current As NuGetVersion = Nothing

        If Not NuGetVersion.TryParse(existingVersion, existing) Then Return True
        If Not NuGetVersion.TryParse(currentVersion, current) Then Return Not ProgramVersionsEqual(existingVersion, currentVersion)

        Return VersionComparer.VersionRelease.Compare(existing, current) < 0
    End Function

End Module
