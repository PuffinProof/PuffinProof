using System.Windows;
using JustSpell.Core;

namespace JustSpell.UI;

public partial class DictionaryWindow : Window
{
    private readonly UserDictionary _user;

    public DictionaryWindow(UserDictionary user)
    {
        InitializeComponent();
        _user = user;
        Refresh();
        NewWordBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OnAdd(this, new RoutedEventArgs());
            }
        };
    }

    private void Refresh() => WordsList.ItemsSource = _user.Words.ToArray();

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        var word = NewWordBox.Text.Trim();
        if (word.Length == 0)
        {
            return;
        }

        _user.Add(word);
        NewWordBox.Clear();
        Refresh();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (WordsList.SelectedItem is string word)
        {
            _user.Remove(word);
            Refresh();
        }
    }
}
