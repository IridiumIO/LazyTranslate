# LazyTranslate


Internal use only for now, and only works for WPF/VB.NET.

LazyTranslate is a TOML-based localisation project for my other WPF applications. It allows source English strings to remain in XAML and Visual Basic code while loading translations from editable catalogue files at runtime.
Initially built as a module in PolyCut, I've extracted and tidied it up to use in other projects

The idea is that you don't have to define resource keys for every string. 
It also allows translations to be updated without recompliation including a remote update mechanism that simply pulls new TOML language definitions. 

This exists because I hate writing code while maintaining language support. Adding `.resx` i18n support to CompactGUI killed most of my motivation to work on it because every change required using my brain to create a resource key and update the english language definition. 
This way I can just append my strings with `.LT()` and it's done. Someone else can translate it later whenever they want, and end users can just download the translation catalogue even if I never push a new build. 

## Simple Usecase
```vb
Public ReadOnly Property ButtonText As String
    Get
        Return "Click me".LT() 'adding LT() will trigger LazyTranslate's localisation
    End Get
End Property
```

```xml
<!-- Before -->
<TextBlock Text="Hello world" />
<Button Content="Save" />
<TextBlock Text="Hello Again" />
<Button Content="{Binding Username, StringFormat={}Hello {0}}" />
<!-- After -->
<TextBlock loc:L.Value="Hello world" /> 
<Button loc:L.Value="Save" />
<TextBlock Text={loc:T Hello Again} />
<Button Content="{loc:Binding Username, Format={}Hello {0}}" />
```

Anything marked will be scanned and added to the base language TOML file (the catalogue)


Note: Most of the readme below this point is AI generated - as stated above, this is an internal project for my other applications to use, so unless the 3 other VB.NET WPF devs in existence find this project, there's no point wasting time on manually writing a readme. If anyone is genuinely interested in this, let me know and I can give you proper instructions or write a proper readme. 

## Projects

- `LazyTranslate` contains the runtime localisation API and WPF markup extensions.
- `LazyTranslate.CatalogueBuilder` scans an application in DEBUG builds and keeps its TOML catalogues synchronised.

## Referencing the projects

Reference the runtime project from the WPF application. Reference the catalogue builder only in DEBUG builds.

```xml
<ItemGroup>
  <ProjectReference Include="..\LazyTranslate\LazyTranslate.vbproj" />
  <ProjectReference Include="..\LazyTranslate.CatalogueBuilder\LazyTranslate.CatalogueBuilder.vbproj"
                    Condition="'$(Configuration)' == 'Debug'" />
</ItemGroup>
```

The string extension methods can be imported for the whole project:

```xml
<ItemGroup>
  <Import Include="LazyTranslate.LocalisationExtensions" />
</ItemGroup>
```

## Catalogue files

Store source catalogues under `Resources\Localisation` in the application project and embed them as resources:

```xml
<ItemGroup>
  <None Remove="Resources\Localisation\*.toml" />
  <EmbeddedResource Include="Resources\Localisation\*.toml" />
</ItemGroup>
```

A basic catalogue looks like this:

```toml
# ExampleApplication Localisation
# @version 1
# @programversion 1.0.0

"Hello world" = "Hello world"
"{0} files selected" = "{0} files selected"

[context."File size"]
"MB" = "MB"
```

Each translation file must be named using its culture code, such as `en-AU.toml`, `de-DE.toml`, or `zh-CN.toml`.

At startup, an on-disk catalogue from an older program version is replaced by the embedded catalogue. For the same program version, it is replaced only when the embedded `# @version` is higher. A catalogue from a newer program version is preserved.

## Initialisation

Initialise LazyTranslate before creating any windows or pages:

```vb
Await LazyTranslate.InitialiseAsync(New LocalisationOptions With {
    .SourceCulture = "en-AU",
    .InitialCulture = settings.Language,
    .CatalogueDirectory = Path.Combine(dataFolder, "Localisation"),
    .ResourceAssembly = GetType(Application).Assembly,
    .ApplicationName = "ExampleApplication",
    .ProgramVersion = AppVersion.ToString()
})
```

`ApplicationName` must match the prefix of the application's embedded resource names. This is normally the application's root namespace.

`ProgramVersion` should be the running application's semantic version. If it is omitted, LazyTranslate uses the resource assembly's informational version.

If the initial culture cannot be loaded, LazyTranslate falls back to the source language.

## XAML usage

Add the LazyTranslate namespace:

```xml
xmlns:loc="clr-namespace:LazyTranslate;assembly=LazyTranslate"
```

Use `loc:L.Value` in place of standard text, content, header, and title properties:

```xml
<!-- Before -->
<TextBlock Text="Hello world" />
<Button Content="Save" />
<!-- After -->
<TextBlock loc:L.Value="Hello world" />
<Button loc:L.Value="Save" />
```

Use `loc:L.Context` when a short or ambiguous string requires a specific translation:

```xml
<TextBlock loc:L.Context="File size" loc:L.Value="MB" />
```

Use `{loc:T ...}` for explicit use anywhere a string can be used:

```xml
<TextBox ToolTip="{loc:T Enter a folder path}" />
```

Use `{loc:Binding ...}` to localise a one-value binding format:

```xml
<TextBlock Text="{loc:Binding FileCount, Format={}{0} files selected}" />
```

The entire format string is the catalogue key. Reusing the same value index is supported:

```xml
<TextBlock Text="{loc:Binding NextRun, Format={}Next scheduled: {0:dd MMM yyyy} at {0:HH:mm:ss}}" />
```

## Visual Basic usage

Use `.LT()` for a direct translation:

```vb
Dim title = "Settings".LT()
```

Pass a context to `.LT()` when required:

```vb
Dim unit = "MB".LT("File size")
```

Use `.LTF()` for a formatted translation:

```vb
Dim message = "{0} files selected".LTF(fileCount)
```

Use `.LTFC()` for a contextual formatted translation:

```vb
Dim message = "{0} saved".LTFC("Storage savings", savedSize)
```

### Bound properties and live language changes

`.LT()`, `.LTF()`, and `.LTFC()` return normal strings. A translated value stored in a property is a snapshot and will not change automatically when another language is loaded:

```vb
Public Property MyName As String = "Button Text".LT()
```

For a literal used by XAML, prefer a LazyTranslate markup extension so it updates automatically:

```xml
<TextBlock Text="{loc:T Button Text}" />
```

If the translated value must be exposed as a bound property, calculate it in the property getter and raise `PropertyChanged` when `L.LanguageChanged` is raised:

```vb
Public ReadOnly Property MyName As String
    Get
        Return "Button Text".LT()
    End Get
End Property

Private Sub LanguageChanged(sender As Object, e As EventArgs)
    RaiseEvent PropertyChanged(
        Me,
        New PropertyChangedEventArgs(NameOf(MyName)))
End Sub
```

Register the handler when the view model is created:

```vb
AddHandler LazyTranslate.L.LanguageChanged, AddressOf LanguageChanged
```

Remove the handler when a reusable or disposable view model is no longer needed.

## Language selection

Get the installed languages with:

```vb
Dim languages = LazyTranslate.GetAvailableLanguages()
```

Load a language from the configured catalogue directory with:

```vb
If L.TryLoadLanguage("de-DE") Then
    settings.Language = "de-DE"
End If
```

WPF values created through LazyTranslate update when the active language changes.

## Remote catalogue updates

LazyTranslate can update a selected on-disk catalogue from a remote `manifest.toml`:

```vb
Dim baseUri As New Uri("https://example.com/localisation/")

If Await LazyTranslate.CheckForLanguageUpdateAsync(settings.Language, baseUri) Then
    L.TryLoadLanguage(settings.Language)
End If
```

The base URI must identify a directory and end with `/`.

The manifest format is:

```toml
schema = 1

[locales."de-DE"]
version = 2
file = "de-DE.toml"
```

The downloaded catalogue's `# @version` must match the version in the manifest, its `# @programversion` must match the running application, and its catalogue version must be newer than the installed file.

## DEBUG catalogue builder

Call the builder after LazyTranslate has been initialised:

```vb
#If DEBUG Then
LazyTranslate.LocalisationCatalogueBuilder.Build(sourceCulture:="en-AU")
#End If
```

The builder scans the calling application project for:

- `loc:L.Value` and `loc:L.Context`
- `{loc:T ...}` and `{loc:Binding ...}`
- `.LT()`, `.LTF()`, and `.LTFC()`

It regenerates the source catalogue, adds missing entries to other language catalogues with `# LOC:TODO`, and moves unused entries into their obsolete sections.
