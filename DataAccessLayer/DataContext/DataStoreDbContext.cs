﻿using System;
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
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.QuestionId).HasName("PK__Question__2EC21549E80C6BEB");

                entity.Property(e => e.QuestionId).HasColumnName("question_id");
                entity.Property(e => e.Answer1)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("answer1");
                entity.Property(e => e.Answer2)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("answer2");
                entity.Property(e => e.Answer3)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("answer3");
                entity.Property(e => e.Answer4)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("answer4");
                entity.Property(e => e.CorrectAnswer)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("correct_answer");
                // Изменили на бинарное представление
                entity.Property(e => e.ImageData)
                    .HasColumnName("image_data")
                    .HasColumnType("varbinary(max)");
                entity.Property(e => e.QuestionText)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("question_text");
                entity.Property(e => e.QuestionExplanation)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("question_explanation");
            });

            modelBuilder.Entity<HardQuestion>(entity =>
            {
                entity.HasKey(e => e.QuestionId).HasName("PK__HardQuestion__2EC21549E80C6BEB");

                entity.Property(e => e.QuestionId).HasColumnName("question_id");
                entity.Property(e => e.QuestionText)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("question_text");
                // Изменили на бинарное представление
                entity.Property(e => e.ImageData)
                    .HasColumnName("image_data")
                    .HasColumnType("varbinary(max)");
                entity.Property(e => e.CorrectAnswer)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("correct_answer");
                entity.Property(e => e.CorrectAnswer2)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("correct_answer2");
            });

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.User)
                .WithMany() // или .WithMany(u => u.QuizResults), если коллекция определена в классе User
                .HasForeignKey(qr => qr.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_QuizResults_AspNetUsers_UserId");

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
