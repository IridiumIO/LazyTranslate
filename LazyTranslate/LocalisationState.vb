Imports System.ComponentModel


Friend NotInheritable Class LocalisationState
    Implements INotifyPropertyChanged

    Public Shared ReadOnly Instance As New LocalisationState()

    Private _version As Integer

    Public ReadOnly Property Version As Integer
        Get
            Return _version
        End Get
    End Property

    Friend Sub Refresh() 'This version is not the same as the output TOML version, this is just to increment for the uI which will probably get confusing...
        _version += 1
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Version)))
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
End Class
