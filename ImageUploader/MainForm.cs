using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
// ���������� ������������ ���, ��� ��������� ��� �������� � ������
using DataAccessLayer.DataContext;      // ��������, ��� ��������� ApplicationDbContext
using DataAccessLayer.Models;    // ��������, ��� �������� ����� Question

namespace ImageUploaderWinForms
{
    public partial class MainForm : Form
    {
        // ��� �������� ���������� ����������� � ���� ������� ������
        private byte[] selectedImageBytes;

        public MainForm()
        {
            LoadQuestions();
        }

        // ����� ��� �������� �������� �� ���� � ComboBox
        private void LoadQuestions()
        {
            using (var db = new DataStoreDbContext())
            {
                // ��������������, ��� � �������� Question ����, ��������, �������� Id � Title (��� Text)
                var questions = db.Questions.ToList();
                comboBoxQuestions.DataSource = questions;
                comboBoxQuestions.DisplayMember = "Title"; // ��� "Text" � � ����������� �� ����� ������
                comboBoxQuestions.ValueMember = "Id";
            }
        }

        // ���������� ������ ������ �����������
        private void buttonSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // ������ ����������� � ������ ������
                    selectedImageBytes = File.ReadAllBytes(ofd.FileName);
                    // ���������� ����������� � PictureBox ��� ���������������� ���������
                    pictureBoxPreview.Image = System.Drawing.Image.FromFile(ofd.FileName);
                }
            }
        }

        // ���������� ������ ���������� ����������� � ����
        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (comboBoxQuestions.SelectedValue == null)
            {
                MessageBox.Show("�������� ������.");
                return;
            }

            if (selectedImageBytes == null)
            {
                MessageBox.Show("�������� �����������.");
                return;
            }

            // �������� Id ���������� �������
            int questionId = Convert.ToInt32(comboBoxQuestions.SelectedValue);

            using (var db = new DataStoreDbContext())
            {
                // ���� ������ �� Id
                var question = db.Questions.FirstOrDefault(q => q.Id == questionId);
                if (question != null)
                {
                    // ���������� �������� ������ ����������� � ���� ImageData
                    question.ImageData = selectedImageBytes;
                    db.SaveChanges();
                    MessageBox.Show("����������� ������� ���������.");
                }
                else
                {
                    MessageBox.Show("������ �� ������.");
                }
            }
        }
    }
}
