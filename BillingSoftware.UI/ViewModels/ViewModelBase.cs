using CommunityToolkit.Mvvm.ComponentModel;

namespace BillingSoftware.UI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
