(function ($) {
    // 1. SERVICES, LOCALIZATION & DOM ELEMENTS
    var _quotationService = abp.services.app.quotation,
        l = abp.localization.getSource("ParkingSystem"),
        _$modal = $("#QuotationEditModal"),
        _$form = _$modal.find("form");

    // 2. FORM VALIDATION (EDIT) - Validated entirely in JS
    _$form.validate({
        rules: {
            VehicleType: {
                required: true,
            },
            DurationUnit: {
                required: true,
            },
            Duration: {
                required: true,
                min:1
            },
            Price: {
                required: true,
                number: true,
                min: 1
            },
           
        }
    });

    // 3. SAVE (UPDATE)
    function save() {
        if (!_$form.valid()) {
            return;
        }

        var quotation = _$form.serializeFormToObject();

        abp.ui.setBusy(_$form);
        _quotationService.update(quotation).done(function () {
            _$modal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            abp.event.trigger("quotation.edited", quotation);
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
