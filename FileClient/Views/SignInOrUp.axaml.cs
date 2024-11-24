using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace FileClient.Views;

public partial class SignInOrUp : UserControl
{
    public ICommand SignInCommand { get; private set;  }
    public ICommand SignUpCommand { get; private set; }
    
    public SignInOrUp(ICommand signInCommand, ICommand signUpCommand)
    {
        SignInCommand = signInCommand;
        SignUpCommand = signUpCommand;
        
        InitializeComponent();
        DataContext = this;
    }
}