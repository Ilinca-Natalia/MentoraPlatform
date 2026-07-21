$(document).ready(function () {
    "use strict";

    const $navbar = $('.navbar');
    const $navLinks = $('.nav-link');

    $(window).on('scroll', function () {
        if ($(this).scrollTop() > 50) {
            $navbar.addClass('navbar-scrolled shadow-lg');
        } else {
            $navbar.removeClass('navbar-scrolled shadow-lg');
        }
    });

    const currentUrl = window.location.pathname.toLowerCase();
    $navLinks.each(function () {
        const href = $(this).attr('href').toLowerCase();
        if (currentUrl.indexOf(href) !== -1 && href !== "/") {
            $(this).addClass('active text-info border-bottom border-info');
        }
    });

    $navLinks.on('click', function () {
        $(this).css('opacity', '0.7');
        setTimeout(() => $(this).css('opacity', '1'), 200);
    });

    console.log("Mentora Platform initialized - Academic Mode Active");
});