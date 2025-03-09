using System;
using System.Collections.Generic;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.DataContext
{
    public partial class DataStoreDbContext : DbContext
    {
        public DataStoreDbContext()
        {
        }

        public DataStoreDbContext(DbContextOptions<DataStoreDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<HardQuestion> HardQuestions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<AdminRegistrationToken> AdminRegistrationTokens { get; set; }
        public virtual DbSet<QuizResult> QuizResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация для таблицы HardQuestions
            modelBuilder.Entity<HardQuestion>(entity =>
            {
                entity.ToTable("HardQuestions");
                entity.HasKey(e => e.QuestionId);

                entity.Property(e => e.QuestionId)
                    .HasColumnName("question_id");

                entity.Property(e => e.QuestionText)
                    .HasColumnName("question_text");

                entity.Property(e => e.CorrectAnswer)
                    .HasColumnName("correct_answer");

                entity.Property(e => e.CorrectAnswer2)
                    .HasColumnName("correct_answer2");

                entity.Property(e => e.QuestionExplanation)
                    .HasColumnName("question_explanation");

                entity.Property(e => e.ImageData)
                    .HasColumnName("image_data");

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasMaxLength(50);
            });

            // Конфигурация для таблицы Questions (Easy и Duel)
            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Questions");
                entity.HasKey(e => e.QuestionId);

                entity.Property(e => e.QuestionId)
                    .HasColumnName("question_id");

                entity.Property(e => e.QuestionText)
                    .HasColumnName("question_text");

                entity.Property(e => e.Answer1)
                    .HasColumnName("answer1");

                entity.Property(e => e.Answer2)
                    .HasColumnName("answer2");

                entity.Property(e => e.Answer3)
                    .HasColumnName("answer3");

                entity.Property(e => e.Answer4)
                    .HasColumnName("answer4");

                entity.Property(e => e.CorrectAnswerIndex)
                    .HasColumnName("correct_answer_index");

                entity.Property(e => e.QuestionExplanation)
                    .HasColumnName("question_explanation");

                entity.Property(e => e.ImageData)
                    .HasColumnName("image_data");

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasMaxLength(50);
            });
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
