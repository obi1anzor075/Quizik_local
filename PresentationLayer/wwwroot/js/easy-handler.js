document.addEventListener('DOMContentLoaded', () => {
    const answers = document.querySelectorAll('.answer');
    const nextQuestionBtn = document.getElementById('next-question-btn');
    let isAnswerSelected = false;

    nextQuestionBtn.setAttribute('disabled', 'true'); // Ensure the button is initially disabled

    function handleAnswerClick(event) {
        const answer = event.currentTarget;
        const url = window.location.href;
        const gameMode = url.substring(url.lastIndexOf('/') + 1);

        if (answer.innerHTML && gameMode) {
            if (!isAnswerSelected) {
                isAnswerSelected = true;
                answer.classList.add('selected');
                const selectedAnswer = answer.textContent.trim(); // Get the selected answer and trim whitespace

                fetch(`/Game/CheckAnswer/${gameMode}/${selectedAnswer}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data.isCorrect) {
                            answer.classList.add('correct');
                        } else {
                            answer.classList.add('incorrect');
                        }
                        // Disable all interactions after selection and dim other answers
                        answers.forEach(ans => {
                            ans.classList.add('disabled');
                            if (ans !== answer) {
                                ans.classList.add('dimmed');
                            } else {
                                ans.classList.remove('dimmed'); // Ensure selected answer is not dimmed
                                ans.classList.remove('disabled'); // Ensure selected answer is not dimmed
                                ans.classList.remove('eventListener');
                            }
                            ans.removeEventListener('click', handleAnswerClick);
                        });
                        // Remove hover and dimming functionality
                        document.querySelectorAll('.answer').forEach(item => {
                            item.removeEventListener('mouseover', handleMouseOver);
                            item.removeEventListener('mouseout', handleMouseOut);
                        });
                        nextQuestionBtn.removeAttribute('disabled'); // Enable the "Continue" button
                    })
                    .catch(error => {
                        console.error('Error:', error);
                    });
            }
        }
    }

    function handleMouseOver(event) {
        if (!isAnswerSelected) {
            document.querySelectorAll('.answer').forEach(element => {
                if (element !== event.currentTarget) {
                    element.classList.add('dimmed');
                }
            });
        }
    }

    function handleMouseOut(event) {
        if (!isAnswerSelected) {
            document.querySelectorAll('.answer').forEach(element => {
                element.classList.remove('dimmed');
            });
        }
    }

    answers.forEach(answer => {
        answer.addEventListener('click', handleAnswerClick);
        answer.addEventListener('mouseover', handleMouseOver);
        answer.addEventListener('mouseout', handleMouseOut);
    });

    // Reset counters when the back button is clicked
    const backButton = document.getElementById('backButton');
    if (backButton) {
        backButton.addEventListener('click', () => {
            fetch('/Game/ResetCounters')
                .catch(error => {
                    console.error('Error:', error);
                });
        });
    }

});
