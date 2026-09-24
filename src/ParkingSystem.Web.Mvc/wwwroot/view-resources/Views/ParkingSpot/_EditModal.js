(function ($) {
    // 1. SERVICES, LOCALIZATION & DOM ELEMENTS
    var _parkingSpotService = abp.services.app.parkingSpot,
        l = abp.localization.getSource("ParkingSystem"),
        _$modal = $("#ParkingSpotEditModal"),
        _$form = _$modal.find("form");

    // 2. FORM VALIDATION (EDIT) - Validated entirely in JS
    _$form.validate({
        rules: {
            SpotCode: {
                required: true,
                maxlength: 30
            }
        }
    });

    // 3. SAVE (UPDATE)
    function save() {
        if (!_$form.valid()) {
            return;
        }

        var parkingSpot = _$form.serializeFormToObject();

        abp.ui.setBusy(_$form);
        _parkingSpotService.update(parkingSpot).done(function () {
            _$modal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            abp.event.trigger("parkingSpot.edited", parkingSpot);
            _$parkingSpotTable.ajax.reload();
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
