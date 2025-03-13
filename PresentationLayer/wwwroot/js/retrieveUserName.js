document.addEventListener("DOMContentLoaded", function () {
    const userName = localStorage.getItem("userName");
    if (userName) {
        document.cookie = `userName=${encodeURIComponent(userName)}; path=/; max-age=31536000;`;
    }
});
