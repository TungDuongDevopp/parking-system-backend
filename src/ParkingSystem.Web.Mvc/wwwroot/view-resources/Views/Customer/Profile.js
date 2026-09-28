(function ($) {
    var _customerService = abp.services.app.customer,
        l = abp.localization.getSource("ParkingSystem"),
        _$form = $("#CustomerProfileForm");

    _$form.validate({
        rules: {
            Name: {
                required: true,
                maxlength: 100
            },
            PhoneNumber: {
                required: true,
                maxlength: 20
            },
            Email: {
                email: true,
                maxlength: 255
            }
        }
    });

    _$form.on("submit", function (e) {
        e.preventDefault();

        if (!_$form.valid()) {
            return;
        }

        var customer = _$form.serializeFormToObject();

        abp.ui.setBusy(_$form);
        _customerService.update(customer).done(function () {
            abp.notify.info(l("SavedSuccessfully"));
        }).always(function () {
            abp.ui.clearBusy(_$form);
        });
    });
})(jQuery);
