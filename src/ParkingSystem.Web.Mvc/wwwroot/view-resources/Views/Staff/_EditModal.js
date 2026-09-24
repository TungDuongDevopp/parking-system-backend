(function ($) {
    // 1. SERVICES, LOCALIZATION & DOM ELEMENTS
    var _staffService = abp.services.app.staff,
        l = abp.localization.getSource("ParkingSystem"),
        _$modal = $("#StaffEditModal"),
        _$form = _$modal.find("form");

    // 2. FORM VALIDATION (EDIT) - Validated entirely in JS
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

    // 3. SAVE (UPDATE)
    function save() {
        if (!_$form.valid()) {
            return;
        }

        var staff = _$form.serializeFormToObject();
        if (staff.Gender === "true") staff.Gender = true;
        else if (staff.Gender === "false") staff.Gender = false;
        else staff.Gender = null;

        abp.ui.setBusy(_$form);
        _staffService.update(staff).done(function () {
            _$modal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            abp.event.trigger("staff.edited", staff);
        }).always(function () {
            abp.ui.clearBusy(_$form);
        });
    }

    // 4. EVENT HANDLERS
    _$form.closest("div.modal-content").find(".save-button").on("click", function (e) {
        e.preventDefault();
        save();
    });

    _$form.find("input").on("keypress", function (e) {
        if (e.which === 13) {
            e.preventDefault();
            save();
        }
    });

    _$modal.on("shown.bs.modal", function () {
        _$form.find("input[type=text]:first").focus();
    });

})(jQuery);
