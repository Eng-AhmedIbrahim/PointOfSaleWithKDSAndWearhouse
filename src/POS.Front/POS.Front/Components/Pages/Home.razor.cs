using System.Text;

namespace POS.Front.Components.Pages;

public partial class Home
{
    private StringBuilder _pin = new();

    private void AddDigit(int digit)
        => _pin.Append(digit);

    private void ClearInput()
     => _pin.Clear();

    private void DeleteLastDigit()
    {
        if (_pin.Length > 0)
            _pin.Remove(_pin.Length - 1, 1);
    }
}