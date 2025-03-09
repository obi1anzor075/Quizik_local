using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Questions")]
public class Question
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; }

    [Column("answer1")]
    public string Answer1 { get; set; }

    [Column("answer2")]
    public string Answer2 { get; set; }

    [Column("answer3")]
    public string Answer3 { get; set; }

    [Column("answer4")]
    public string Answer4 { get; set; }

    [Column("correct_answer_index")]
    public int CorrectAnswerIndex { get; set; }

    [Column("question_explanation")]
    public string QuestionExplanation { get; set; }

    [Column("image_data")]
    public byte[] ImageData { get; set; }

    [Column("category")]
    public string Category { get; set; }
}
