using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
// Подключите пространства имён, где находятся ваш контекст и модели
using DataAccessLayer.DataContext;      // Например, где находится ApplicationDbContext
using DataAccessLayer.Models;    // Например, где определён класс Question

namespace ImageUploaderWinForms
{
    public partial class MainForm : Form
    {
        // Для хранения выбранного изображения в виде массива байтов
        private byte[] selectedImageBytes;

        public MainForm()
        {
            LoadQuestions();
        }

        // Метод для загрузки вопросов из базы в ComboBox
        private void LoadQuestions()
        {
            using (var db = new DataStoreDbContext())
            {
                // Предполагается, что у сущности Question есть, например, свойства Id и Title (или Text)
                var questions = db.Questions.ToList();
                comboBoxQuestions.DataSource = questions;
                comboBoxQuestions.DisplayMember = "Title"; // или "Text" – в зависимости от вашей модели
                comboBoxQuestions.ValueMember = "Id";
            }
        }

        // Обработчик кнопки выбора изображения
        private void buttonSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Чтение изображения в массив байтов
                    selectedImageBytes = File.ReadAllBytes(ofd.FileName);
                    // Отображаем изображение в PictureBox для предварительного просмотра
                    pictureBoxPreview.Image = System.Drawing.Image.FromFile(ofd.FileName);
                }
            }
        }

        // Обработчик кнопки сохранения изображения в базе
        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (comboBoxQuestions.SelectedValue == null)
            {
                MessageBox.Show("Выберите вопрос.");
                return;
            }

            if (selectedImageBytes == null)
            {
                MessageBox.Show("Выберите изображение.");
                return;
            }

            // Получаем Id выбранного вопроса
            int questionId = Convert.ToInt32(comboBoxQuestions.SelectedValue);

            using (var db = new DataStoreDbContext())
            {
                // Ищем вопрос по Id
                var question = db.Questions.FirstOrDefault(q => q.Id == questionId);
                if (question != null)
                {
                    // Записываем бинарные данные изображения в поле ImageData
                    question.ImageData = selectedImageBytes;
                    db.SaveChanges();
                    MessageBox.Show("Изображение успешно сохранено.");
                }
                else
                {
                    MessageBox.Show("Вопрос не найден.");
                }
            }
        }
    }
}
