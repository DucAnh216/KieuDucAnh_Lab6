(function($){
    $(function(){

        // Khởi tạo hiệu ứng MaterializeCSS
        $('.sidenav').sidenav();
        $('.parallax').parallax();

        // Ngăn chặn nút Back và Forward trên trình duyệt
        if (window.history && window.history.pushState) {
            window.history.pushState('forward', '', window.location.href);
            $(window).on('popstate', function (e) {
                window.history.pushState('forward', '', window.location.href);
                e.preventDefault();
            });
        }

        // Ngăn chặn chuột phải trên toàn bộ cửa sổ
        $(document).ready(function () {
            $(window).on("contextmenu", function () {
                return false;
            });
        });

        // Chặn Developer Tools (F12, Ctrl+Shift+I, Ctrl+Shift+J)
        $(document).keydown(function (event) {
            if (event.keyCode == 123) { // F12
                return false;
            }
            if (event.ctrlKey && event.shiftKey && (event.keyCode == 73 || event.keyCode == 74)) {
                return false;
            }
        });

        // Chặn Inspect Element (Ctrl+Shift+C)
        document.addEventListener('keydown', function (e) {
            if (e.ctrlKey && e.shiftKey && e.keyCode === 67) {
                e.preventDefault();
            }
        });

    }); // End of document ready
})(jQuery); // End of jQuery namespace
