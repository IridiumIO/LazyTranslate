Imports System.Reflection

Public NotInheritable Class LocalisationOptions
    Public Property SourceCulture As String = "en-AU"
    Public Property InitialCulture As String
    Public Property CatalogueDirectory As String
    Public Property ResourceAssembly As Assembly
    Public Property ApplicationName As String
    Public Property ProgramVersion As String
End Class
