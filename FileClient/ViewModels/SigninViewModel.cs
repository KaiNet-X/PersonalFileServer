namespace FileClient.ViewModels;

using FileClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Common;

public class SigninViewModel : ViewModelBase
{
    public string Username { get; set; }
    public string Password { get; set; }

    private readonly AuthService _authService;

    public SigninViewModel()
    {
        _authService = Extensions1.ServiceProvider.GetService<AuthService>();
    }

    public async Task SignIn()
    {
        await _authService.SetUser(new User(Username, Password));
    }
}