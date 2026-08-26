Imports System.IO

Class Application
    Private Async Sub Application_Startup(sender As Object, e As StartupEventArgs)

        Await LazyTranslate.InitialiseAsync(New LocalisationOptions With {
            .SourceCulture = "en-AU",
            .InitialCulture = "en-AU",
            .CatalogueDirectory = "Localisation",
            .ResourceAssembly = GetType(Application).Assembly,
            .ApplicationName = "LazyTranslate.WPFTest"
        })

#If DEBUG Then
        LocalisationCatalogueBuilder.Build(sourceCulture:="en-AU")
#End If


    End Sub

End Class
