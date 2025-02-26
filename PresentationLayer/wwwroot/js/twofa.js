// Скрипт для отправки кода 2FA на сервер
function verify2FACode() {
    var code = $('#verificationCode').val();
    var button = $('#button-play'); // Кнопка с id 'button-play'

    // Добавляем класс для пульсации, кнопка начинает пульсировать
    button.addClass('button-pulse');
        button.text("Подождите...");

    $.ajax({
        type: "POST",
        url: "/Home/Verify2FACode",
        data: { code: code },
        success: function (response) {
            if (response.success) {
                $('#verifyMessage').removeClass('text-danger').addClass('text-success').text(response.message);
                setTimeout(function () {
                    location.reload();
                }, 2000);
            } else {
                $('#verifyMessage').removeClass('text-success').addClass('text-danger').text(response.message);
            }
        },
        error: function () {
            $('#verifyMessage').removeClass('text-success').addClass('text-danger').text('Ошибка сервера. Повторите попытку.');
        },
        complete: function() {
            // Убираем анимацию пульсации, когда запрос завершен
            button.removeClass('button-pulse');
            button.text("Подтвердить");
        }
    });
}

// Отключение 2FA
    function disable2FA() {
        var button = $('#button-cancel'); // Кнопка с id 'button-cancel'

        // Добавляем класс для пульсации, кнопка начинает пульсировать
        button.addClass('button-pulse-invert');
        button.text("Подождите..."); // Изменяем текст кнопки на "Подождите"

        $.ajax({
            type: "POST",
        url: "/Home/Disable2FA", // URL метода отключения 2FA
        success: function (response) {
                if (response.success) {
            $('#verifyMessage').removeClass('text-danger').addClass('text-success').text(response.message);
        setTimeout(function () {
            location.reload();
                    }, 2000);
                } else {
            $('#verifyMessage').removeClass('text-success').addClass('text-danger').text(response.message);
                }
            },
        error: function () {
            $('#verifyMessage').removeClass('text-success').addClass('text-danger').text('Ошибка сервера. Повторите попытку.');
            },
        complete: function () {
            // Убираем анимацию пульсации, когда запрос завершен
            button.removeClass('button-pulse-invert');
        button.text("Отменить"); // Восстанавливаем исходный текст кнопки
            }
        });
    }
