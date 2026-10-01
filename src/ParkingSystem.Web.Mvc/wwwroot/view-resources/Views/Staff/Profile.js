(function ($) {
    var _staffService = abp.services.app.staff,
        l = abp.localization.getSource("ParkingSystem"),
        _$form = $("#StaffProfileForm");

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
            },
             HiredDate: {
                required: true
            }
        }
    });

    _$form.on("submit", function (e) {
        e.preventDefault();

        if (!_$form.valid()) {
            return;
        }

        var staff = _$form.serializeFormToObject();
        if (staff.Gender === "true") staff.Gender = true;
        else if (staff.Gender === "false") staff.Gender = false;
        else staff.Gender = null;
        abp.ui.setBusy(_$form);
        _staffService.changeProfile(staff).done(function () {
            abp.notify.info(l("SavedSuccessfully"));
        }).always(function () {
            abp.ui.clearBusy(_$form);
        });
    });
})(jQuery);

