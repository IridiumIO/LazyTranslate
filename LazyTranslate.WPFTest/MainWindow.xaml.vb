Class MainWindow

    Public Property MyName As String = "Button Text".LT()

    Public Sub New()

        InitializeComponent()

        Me.DataContext = Me

    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)

        Dim languages = LazyTranslate.GetAvailableLanguages()

        If Keyboard.IsKeyDown(Key.G) Then
            LazyTranslate.L.TryLoadLanguage("de-DE")
        Else
            LazyTranslate.L.TryLoadLanguage("en-AU")

        End If


    End Sub
End Class
