// JavaScript код для работы с SignalR и другими функциями
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")
    .build();

let chatRoom = '';
let selectedAnswer = '';
let userName = '';
let isJoining = false;
let timerInterval;

// Получаем значение режима из data-атрибута тега body
const gameMode = document.body.getAttribute('data-gamemode');

connection.on("ReceiveQuestion", function (questionId, questionText, imageUrl,questionExplanation ,answers) {
    document.getElementById('card-login').style.display = 'none';
    console.log("Received question:", questionId, questionText, imageUrl, answers);
    document.getElementById('questionId').innerText = `Вопрос ${questionId}`;
    document.getElementById('questionText').innerText = questionText;
    document.getElementById('questionText').setAttribute('data-question-id', questionId);
    if (imageUrl) {
        document.getElementById('questionImage').src = imageUrl;
        document.getElementById('questionImage').style.display = 'block';
    } else {
        document.getElementById('questionImage').style.display = 'none';
    }

    document.getElementById('joinDuelBtn').style.display = 'none';

    const answersList = document.getElementById('answersList');
    answersList.innerHTML = '';

    // Shuffle answers randomly
    answers = shuffle(answers);

    const column1 = document.createElement('div');
    const column2 = document.createElement('div');
    column1.classList.add('wrapper-answer');
    column2.classList.add('wrapper-answer');

    answers.forEach((answer, index) => {
        const li = document.createElement('li');
        li.textContent = answer;
        li.classList.add('answer');
        li.classList.add('eventListener');
        li.addEventListener('click', async function () {
            selectedAnswer = answer;
            document.querySelectorAll('#answersList .answer').forEach(item => {
                item.classList.remove('selected');
            });
            li.classList.add('selected');
            await submitAnswer(questionId, selectedAnswer);
        });
        li.addEventListener('mouseover', function () {
            document.querySelectorAll('#answersList .answer').forEach(item => {
                if (item !== li) {
                    item.classList.add('dimmed');
                }
            });
        });
        li.addEventListener('mouseout', function () {
            document.querySelectorAll('#answersList .answer').forEach(item => {
                item.classList.remove('dimmed');
            });
        });
        if (index % 2 === 0) {
            column1.appendChild(li);
        } else {
            column2.appendChild(li);
        }
    });

    answersList.appendChild(column1);
    answersList.appendChild(column2);

    // Запуск таймера только при получении первого вопроса
    if (!timerInterval) {
        startTimer(45000); // Specify the duration in seconds
    }
});

// Function to shuffle array randomly
function shuffle(array) {
    for (let i = array.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [array[i], array[j]] = [array[j], array[i]];
    }
    return array;
}

connection.on("AnswerResult", function (isCorrect) {
    const selectedElement = document.querySelector('.answer.selected');
    if (selectedElement) {
        selectedElement.classList.add(isCorrect ? 'correct' : 'incorrect');
    }
});

connection.on("GameReady", function () {
    console.log("Both players are ready!");
    document.getElementById('questionSection').style.display = 'flex';
    document.getElementById('question').style.display = 'flex';
});

connection.on("StartGame", function () {
    Cookies.set("questionIndex", 0);
    console.log("Game has started!");
    document.getElementById('questionSection').style.display = 'block';
    document.getElementById('question').style.display = 'flex';
    getNextQuestion();
});

// Ensure this script is loaded and confettiKit is defined before this code runs
connection.on("EndGame", function (results) {
    console.log('Received results:', results); // Логируем полученные результаты

    // Находим элементы
    const questionSection = document.getElementById('questionSection');
    const question = document.getElementById('question');
    const finishLevelContainer = document.querySelector('.finish__level_card');

    // Скрываем секцию вопроса
    questionSection.style.display = 'none';
    question.style.display = 'none';

    // Очищаем предыдущий контент
    finishLevelContainer.innerHTML = '';
    document.getElementById('finish__level_card_wrapper').style.display = 'flex';

    // Убедимся, что результаты включают двух игроков
    const players = Object.keys(results);
    if (players.length !== 2) {
        console.error('Results do not include exactly two players:', results);
        return;
    }

    try {
        // Создаем и добавляем заголовок результата
        const resultHeader = document.createElement('div');
        resultHeader.className = 'finish__text';
        finishLevelContainer.appendChild(resultHeader);

        // Получаем имена игроков и их результаты
        const player1 = players[0];
        const player2 = players[1];

        const score1 = results[player1];
        const score2 = results[player2];

        // Логируем результаты для отладки
        console.log(`Player 1: ${player1}, Score: ${score1}`);
        console.log(`Player 2: ${player2}, Score: ${score2}`);

        // Создаем элементы для отображения результатов игроков
        const player1Result = document.createElement('div');
        player1Result.className = 'finish__level';
        player1Result.textContent = `Игрок: ${player1}, Счет: ${score1}`;

        const player2Result = document.createElement('div');
        player2Result.className = 'finish__level';
        player2Result.textContent = `Игрок: ${player2}, Счет: ${score2}`;

        // Сравниваем очки и добавляем соответствующие классы
        if (score1 > score2) {
            player1Result.classList.add('winner');
            resultHeader.textContent = `Победил ${player1}!`;
            player2Result.classList.add('looser');
            finishLevelContainer.appendChild(player1Result);
            finishLevelContainer.appendChild(player2Result);
        } else {
            player2Result.classList.add('winner');
            resultHeader.textContent = `Победил ${player2}!`;
            player1Result.classList.add('looser');
            finishLevelContainer.appendChild(player2Result);
            finishLevelContainer.appendChild(player1Result);
        }

        // Создаем и добавляем кнопку возврата в главное меню
        const returnButton = document.createElement('a');
        returnButton.href = '/Home/SelectMode';
        returnButton.className = 'button return__button';
        returnButton.textContent = 'Главное меню';
        finishLevelContainer.appendChild(returnButton);

        // Логируем завершение игры и результаты
        console.log('Game Ended:', results);
    } catch (error) {
        console.error('Error while processing results:', error);
    }

    // Очистка интервала таймера
    clearInterval(timerInterval);
    timerInterval = null;

    console.log('END');
    // Trigger confetti animation
    new confettiKit({
        confettiCount: 40,
        angle: 60,
        startVelocity: 80,
        colors: randomColor({ hue: 'blue', count: 18 }),
        elements: {
            'confetti': {
                direction: 'down',
                rotation: true,
            },
            'star': {
                count: 10,
                direction: 'down',
                rotation: true,
            },
            'ribbon': {
                count: 5,
                direction: 'down',
                rotation: true,
            },
            'custom': [{
                count: getRandomInt(2, 4),
                width: 50,
                textSize: 15,
                content: '//bootstraptema.ru/snippets/effect/2018/confettikit/shar.png',
                contentType: 'image',
                direction: 'up',
                rotation: false,
            }]
        },
        position: 'bottomLeftRight',
    });
});


// Helper function to get random integer between min and max (inclusive)
function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

async function joinDuel() {
    if (isJoining) return; // Prevent multiple join attempts
    isJoining = true;

    await connection.start();

    try {
        await retrieveUserName(); // Получаем имя пользователя
        chatRoom = document.getElementById('input__room').value;
        await connection.invoke("JoinDuel", { UserName: userName, ChatRoom: chatRoom });
        console.log("Connected and joined duel");
        document.getElementById('waiting-card').style.display = 'none';
    } catch (err) {
        console.error("Error in joinDuel:", err);
    } finally {
        isJoining = false; // Reset flag after joining attempt
    } 
    document.getElementById('duel-log').style.display = 'none';
    document.getElementById('waiting-card').style.display = 'flex';
}

// Функция для получения следующего вопроса
async function getNextQuestion() {
    try {
        const questionId = parseInt(document.getElementById('questionText').getAttribute('data-question-id')) || 0;
        console.log(`Requesting next question: index=${questionId}`);
        await connection.invoke("GetNextQuestion", userName, chatRoom, questionId, gameMode);
    } catch (err) {
        console.error(err);
    }
}

// Функция для отправки ответа
async function submitAnswer(questionId, answer) {
    try {
        console.log(`Submitting answer: UserName=${userName}, ChatRoom=${chatRoom}, QuestionId=${questionId}, Answer=${answer}`);
        await connection.invoke("AnswerQuestion", userName, chatRoom, questionId, answer, gameMode);
    } catch (err) {
        console.error("Error invoking AnswerQuestion:", err);
    }
}

async function endGame() {
    try {
        Cookies.remove("questionIndex");
        await connection.invoke("EndGame", chatRoom); // Ensure chatRoom is passed
    } catch (err) {
        console.error("Error invoking EndGame:", err);
    }
}

async function retrieveUserName() {
    try {
        // Вызываем метод GetUserName на сервере и ждем ответа
        userName = await connection.invoke('GetUserName');
        console.log("Retrieved user name: " + userName);

        // Теперь можно продолжить работу с именем пользователя
        // Например, отобразить его на странице или использовать в другой логике
    } catch (err) {
        console.error("Error retrieving user name:", err.toString());
    }
}

// Timer function
function startTimer(durationInSeconds) {
    console.log("Timer started for", durationInSeconds, "seconds");
    const timerBar = document.getElementById('timerBar');
    const totalWidth = document.getElementById('timeBarContainer').offsetWidth;

    // Инициализируем начальное состояние
    timerBar.style.width = totalWidth + 'px';

    let currentTime = durationInSeconds;

    timerInterval = setInterval(() => {
        currentTime -= 1;
        const newWidth = (currentTime / durationInSeconds) * totalWidth;
        
       //console.log("Timer update: currentTime =", currentTime, "newWidth =", newWidth);
        
        timerBar.style.width = newWidth + 'px';

        if (currentTime <= 0) {
            clearInterval(timerInterval);
            timerBar.style.width = '0px';
            endGame(); // Call end game function
        }
    }, 1000);
}

function getCookie(name) {
    let matches = document.cookie.match(new RegExp(
        "(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
    ));
    return matches ? decodeURIComponent(matches[1]) : null;
}

document.getElementById('joinDuelBtn').addEventListener('click', joinDuel);
connection.on("RoomFull", function () {
    document.getElementById('error').style.display = 'block';
    window.location.reload();
});