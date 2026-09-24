(function ($) {
    // 1. SERVICES, LOCALIZATION & DOM ELEMENTS
    var _parkingAreaService = abp.services.app.parkingArea,
        l = abp.localization.getSource("ParkingSystem"),
        _$modal = $("#ParkingAreaEditModal"),
        _$form = _$modal.find("form");

    // 2. FORM VALIDATION (EDIT) - Validated entirely in JS
    _$form.validate({
        rules: {
            ParkingCode: {
                required: true,
                maxlength: 30
            },
            Name: {
                required: true,
                maxlength: 30
            },
            VehicleType: {
                required: true
            },
            Capacity: {
                required: true,
                number: true,
                min: 1
            },
            ParkingMode: {
                required: true
            },
            Location: {
                required: true,
                maxlength: 255
            },
            Description: {
                maxlength: 255
            }
        }
    });

    // 3. SAVE (UPDATE)
    function save() {
        if (!_$form.valid()) {
            return;
        }

        var parkingArea = _$form.serializeFormToObject();

        abp.ui.setBusy(_$form);
        _parkingAreaService.update(parkingArea).done(function () {
            _$modal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            abp.event.trigger("parkingArea.edited", parkingArea);
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
