using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("HardQuestions")]
public class HardQuestion
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; }

    [Column("correct_answer")]
    public string CorrectAnswer { get; set; }

    [Column("correct_answer2")]
    public string CorrectAnswer2 { get; set; }

    [Column("question_explanation")]
    public string QuestionExplanation { get; set; }

    [Column("image_data")]
    public byte[] ImageData { get; set; }

    [Column("category")]
    public string Category { get; set; }
}
