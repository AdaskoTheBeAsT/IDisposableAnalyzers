namespace ValidCode.Rx;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class NotifyPropertyChanged : INotifyPropertyChanged
{
    private int _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set
        {
            if (value == _value)
            {
                return;
            }

            _value = value;
            this.OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
