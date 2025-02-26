// Логика смены пароля и проверки в реальном времени
        $(document).ready(function () {
            // Проверка текущего пароля через AJAX
            $('#currentPassword').on('blur', function () {
                var currentPassword = $(this).val();

                if (currentPassword.length > 0) {
                    $.ajax({
                        type: "POST",
                        url: "/Home/VerifyCurrentPassword", // URL для проверки пароля
                        data: { currentPassword: currentPassword },
                        success: function (response) {
                            if (!response.success) {
                                $('#currentPasswordError').text('Неверный текущий пароль');
                            } else {
                                $('#currentPasswordError').text('');
                            }
                        },
                        error: function () {
                            $('#currentPasswordError').text('Ошибка сервера');
                        }
                    });
                }
            });

        // Проверка совпадения паролей
        $('#confirmPassword').on('blur', function () {
            var newPassword = $('#newPassword').val();
        var confirmPassword = $(this).val();

        if (newPassword !== confirmPassword) {
            $('#confirmPasswordError').text('Пароли не совпадают');
            } else {
            $('#confirmPasswordError').text('');
            }
        });

        // Отправка формы
        $('#changePasswordForm').on('submit', function (event) {
            event.preventDefault(); // Предотвращаем отправку формы

        var currentPassword = $('#currentPassword').val();
        var newPassword = $('#newPassword').val();
        var confirmPassword = $('#confirmPassword').val();
        $('#passwordError').removeClass('success');
        $('#passwordError').text('');

        // Валидация данных
        if (currentPassword.length == 0) {
            $('#currentPasswordError').text('Текущий пароль обязателен');
        return;
            }

        if (newPassword.length == 0) {
            $('#newPasswordError').text('Новый пароль обязателен');
        return;
            }

        if (newPassword !== confirmPassword) {
            $('#confirmPasswordError').text('Пароли не совпадают');
        return;
            }

        // Отправка данных
        $.ajax({
            type: "POST",
        url: "/Home/ChangePassword", // URL для смены пароля
        data: {
            currentPassword: currentPassword,
        newPassword: newPassword
                },
        success: function (response) {
                    if (response.success) {
            $('#passwordError').addClass('success');
        $('#passwordError').text('Пароль успешно изменен.');
                        // Перенаправление или другие действия после успешного изменения пароля
                    } else {
            $('#passwordError').text('Пароль не соответсвет требованиям.');
                    }
                },
        error: function () {
            $('#passwordError').text('Ошибка сервера');
                }
            });
        });
    });