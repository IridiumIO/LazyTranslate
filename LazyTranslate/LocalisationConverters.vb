Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data


Friend NotInheritable Class TranslationConverter
    Implements IValueConverter

    Private ReadOnly _source As String
    Private ReadOnly _context As String

    Public Sub New(source As String, context As String)
        _source = source
        _context = context
    End Sub

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return L.T(_source, _context)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return System.Windows.Data.Binding.DoNothing
    End Function
End Class


Friend NotInheritable Class LocalisedFormatConverter
    Implements IMultiValueConverter

    Private ReadOnly _format As String
    Private ReadOnly _context As String
    Private ReadOnly _valueCount As Integer

    Public Sub New(format As String, context As String, valueCount As Integer)
        _format = format
        _context = context
        _valueCount = valueCount
    End Sub

    Public Function Convert(values As Object(), targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert

        If values.Length < _valueCount Then Return DependencyProperty.UnsetValue

        Dim formatValues(_valueCount - 1) As Object

        For index = 0 To _valueCount - 1
            If values(index) Is DependencyProperty.UnsetValue Then Return DependencyProperty.UnsetValue
            formatValues(index) = values(index)
        Next

        Return String.Format(L.CurrentCulture, L.T(_format, _context), formatValues)

    End Function

    Public Function ConvertBack(value As Object, targetTypes As Type(), parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class
