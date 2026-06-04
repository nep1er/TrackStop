using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class CreateAlbumWindow : Window
    {
        private readonly int _artistId;
        private readonly AlbumRepository _albumRepo = new AlbumRepository();
        private string _selectedCoverPath = "";

        public CreateAlbumWindow(int artistId)
        {
            InitializeComponent();
            _artistId = artistId;
        }

        private void SelectCoverButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png",
                Title = "Выберите обложку альбома"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedCoverPath = openFileDialog.FileName;
                CoverPathText.Text = Path.GetFileName(_selectedCoverPath);

                // Показываем превью
                CoverPreview.Source = new BitmapImage(new Uri(_selectedCoverPath));
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var title = AlbumTitleTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название альбома", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(_selectedCoverPath))
            {
                MessageBox.Show("Выберите обложку для альбома", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Копируем обложку в папку приложения
                string coversFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers");
                if (!Directory.Exists(coversFolder))
                    Directory.CreateDirectory(coversFolder);

                string fileName = $"album_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(_selectedCoverPath)}";
                string finalCoverPath = Path.Combine(coversFolder, fileName);
                File.Copy(_selectedCoverPath, finalCoverPath, true);

                // Создаем альбом в базе данных
                int albumId = _albumRepo.CreateAlbum(title, finalCoverPath, _artistId);

                if (albumId > 0)
                {
                    MessageBox.Show("Альбом успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось создать альбом", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании альбома: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}