Imports System.Globalization

Public NotInheritable Class LanguageItem
    Public Sub New(cultureCode As String)
        Dim culture = CultureInfo.GetCultureInfo(cultureCode)
        Me.CultureCode = culture.Name
        NativeName = culture.NativeName
        Name = culture.NativeName
        ISOCountryCode = If(culture.Name.Contains("-"c), culture.Name.Split("-"c).Last(), culture.Name)
    End Sub

    Public ReadOnly Property CultureCode As String
    Public ReadOnly Property NativeName As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property ISOCountryCode As String
End Class
