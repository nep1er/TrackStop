using System.Windows;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class LoginWindow : Window
    {
        private readonly UserRepository _userRepo = new UserRepository();

        public LoginWindow()
        {
            InitializeComponent();
            LoginTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptRegister();
        }

        private void AttemptLogin()
        {
            var login = LoginTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Введите логин и пароль");
                return;
            }

            var user = _userRepo.Authenticate(login, password);
            if (user != null)
            {
                // Сохраняем данные пользователя в статическом классе App
                App.CurrentUserId = user.UserId;
                App.CurrentUserLogin = user.Login;
                App.CurrentUserRole = user.Role;

                // Открываем главное окно
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowMessage("Неверный логин или пароль");
            }
        }

        private void AttemptRegister()
        {
            var login = LoginTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Введите логин и пароль");
                return;
            }

            if (login.Length < 3)
            {
                ShowMessage("Логин должен содержать минимум 3 символа");
                return;
            }

            if (password.Length < 4)
            {
                ShowMessage("Пароль должен содержать минимум 4 символа");
                return;
            }

            if (_userRepo.CreateUser(login, password))
            {
                ShowMessage("Аккаунт успешно создан! Теперь войдите.", isSuccess: true);
                PasswordBox.Password = "";
            }
            else
            {
                ShowMessage("Логин уже занят");
            }
        }

        private void ShowMessage(string message, bool isSuccess = false)
        {
            MessageText.Text = message;
            MessageText.Foreground = isSuccess ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;
        }

        private void LoginTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AttemptLogin();
            }
        }
    }
}