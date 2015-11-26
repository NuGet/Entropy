// Global utility script for the Gallery
/// <reference path="jquery-1.11.2.js" />
(function (window, $) {
    function sniffClickonce() {
        var userAgent = window.navigator.userAgent.toUpperCase(),
            hasNativeDotNet = userAgent.indexOf('.NET CLR 3.5') >= 0;

        if (hasNativeDotNet) {
            $('.s-noclickonce').removeClass('s-noclickonce');
        }
    }

    $(function () {
        sniffClickonce();

        // Add validator that ensures provided value is NOT equal to a specified value.
        $.validator.addMethod('notequal', function (value, element, params) {
            return value !== params;
        });

        // -- responsive menu --
        $('.nav-toggle').on('click', function (e) {
            $target = $($(this).data('toggle'));
            if ($target) {
                $target.slideToggle();
            }

            $('.nav-toggle').each(function (i, item) {
                $ele = $($(item).data('toggle'));
                if ($ele && !$ele.is($target)) {
                    $ele.slideUp();
                }
            });
            e.preventDefault();
        });

        $(window).on('resize', function () {
            var w = $(window).width();

            $('.nav-toggle').each(function (i, item) {
                $target = $($(item).data('toggle'));
                if ($target && w > 600) {
                    $target.show();
                } else if ($target && w <= 600 && $target.is(':visible')) {
                    $target.slideUp();
                }
            });
        });
        // -- / responsive menu --

        // -- collapse/expand --
        $('.expand').on('click', function (e) {
            $self = $(this);
            $target = $($(this).data('expand'));
            if ($target) {
                $target.slideToggle(function () {
                    if ($target.is(':visible')) {
                        var text = $self.data('expand-hide');
                        if (text) {
                            $self.text(text);
                        }
                    } else {
                        var text = $self.data('expand-show');
                        if (text) {
                            $self.text(text);
                        }
                    }
                });
            }
            e.preventDefault();
        });
        // -- / collapse/expand --

        // -- upload --
        $('input[type="file"]').on('change', function (e) {
            $(this).parent().parent().find('.uploadfile').text(this.value.replace('C:\\fakepath\\', ''));
        });
        // -- /upload --

		// -- destructive actions --
		$('.destructive').on('click', function (e) {
            if (!confirm('Are you sure? This can not be undone.')) {
	            e.preventDefault();
            }
        });
		// -- /destructive actions --

        // -- search --
        $('.search-list').on('keyup', function(e) {
            var term = $(this).val().toLowerCase();

            $(this).closest('ul').find('li').each(function (i, item) {
                var search = $(item).data('search');

                if (term != '' && search && search.toLowerCase().indexOf(term) == -1 && !$(this).hasClass('current')) {
                    $(item).hide();
                } else {
                    $(item).show();
                }
            });
        });
        // -- / search -
    });

})(window, jQuery);