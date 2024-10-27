document.addEventListener('DOMContentLoaded', () => {
    const nextQuestionBtn = document.getElementById('next-question-btn');
    const answerInput = document.querySelector('.input-answer');
    const confirmBtn = document.querySelector('.confirm-btn');
    let isAnswerSelected = false;

    // Disable "Continue" button initially
    nextQuestionBtn.setAttribute('disabled', 'true');

    function handleAnswerInput() {
        const selectedAnswer = answerInput.value.trim();
        const url = window.location.href;
        const gameMode = url.substring(url.lastIndexOf('/') + 1);

        if (selectedAnswer && gameMode) {
            if (!isAnswerSelected) {
                isAnswerSelected = true;
                confirmBtn.classList.add('disabled'); // Prevent multiple clicks

                fetch(`/Game/CheckHardAnswer/${gameMode}/${selectedAnswer}`)
                    .then(response => {
                        if (!response.ok) {
                            // Если ответ не успешен, выбрасываем ошибку с текстом ответа
                            return response.text().then(text => { throw new Error(text); });
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (data.isCorrect) {
                            answerInput.classList.add('correct');
                        } else {
                            answerInput.classList.add('incorrect');
                        }
                        confirmBtn.classList.add('disabled'); // Disable the confirm button
                        answerInput.setAttribute('readonly', 'true'); // Prevent further input

                        nextQuestionBtn.removeAttribute('disabled'); // Enable "Continue" button
                    })
                    .catch(error => {
                        console.error('Error:', error.message || error);
                        isAnswerSelected = false; // Reset selection flag in case of error
                    });
            }
        }
    }


    if (answerInput && confirmBtn) {
        answerInput.addEventListener('keypress', (event) => {
            if (event.key === 'Enter' && !confirmBtn.classList.contains('disabled') && !isAnswerSelected) {
                event.preventDefault();
                handleAnswerInput();
            }
        });

        confirmBtn.addEventListener('click', (event) => {
            event.preventDefault();
            if (!isAnswerSelected) {
                handleAnswerInput();
            }
        });
    }

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
