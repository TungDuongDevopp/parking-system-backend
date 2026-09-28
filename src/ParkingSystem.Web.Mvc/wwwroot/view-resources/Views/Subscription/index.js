(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _subscriptionService = abp.services.app.subscription,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#QuotationCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#SubscriptionTable"),
        _$searchForm = $("#SubscriptionSearchForm");
     
      canDelete = isManager;

    // 3. DATATABLE INITIALIZATION
    var _$subscriptionTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _subscriptionService.getAll,
            inputFilter: function () {
                var filter = _$searchForm.serializeFormToObject(true);
                for (var key in filter) {
                    if (filter[key] === "") delete filter[key];
                }
                return filter;
            }
        },
        buttons: [
            {
                name: "refresh",
                text: '<i class="fas fa-redo-alt"></i>',
                action: function () {
                    _$subscriptionTable.draw(false);
                }
            }
        ],
        responsive: {
            details: {
                type: "column"
            }
        },
        columnDefs: [
            { targets: 0, className: "control", defaultContent: "", orderable: false },
            { targets: 1, data: "customerName",name:"customerName"},
            { targets: 2, data: "vehicleTypeName", name:"vehicleType" },
            { targets: 3, data: "duration",
            render: function (data, type, row) {
             return data + " " + row.durationUnitName;
            }
        
        },
            { targets: 4, data: "price"},
            { targets: 5, data: "startTime",
             render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }    
            },
            { targets: 6, data: "endTime",
                  render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            { targets: 7, data: "status", name:"status",
                render: function (data, type, row) {
                if (data === 0) {
                    return '<span class="badge badge-warning bg-warning">'
                        + l("Pending") + '</span>';
                }

                if (data === 1) {
                    return '<span class="badge badge-success bg-success">'
                        + l("InUse") + '</span>';
                }

                if (data === 2) {
                    return '<span class="badge badge-secondary bg-secondary">'
                        + l("Expired") + '</span>';
                }

                if (data === 3) {
                    return '<span class="badge badge-danger bg-danger">'
                        + l("Canceled") + '</span>';
                }
                return row.statusName || "";
            }
        
        }
        ]
    });

    // 4. FORM VALIDATION (CREATE) - Validated entirely in JS
    // _$createForm.validate({
    //     rules: {
    //         VehicleType: {
    //             required: true,   
    //         },
    //         DurationUnit: {
    //             required: true,  
    //         },
    //         Duration: {
    //             required: true,
    //             number: true,
    //             min: 1
    //         },
    //         Price: {
    //             required: true,
    //             number: true,
    //             min: 1
    //         }
    //     }
    // });

    // 6. CREATE (SAVE)
    // _$createForm.find(".save-button").on("click", function (e) {
    //     e.preventDefault();
    //     if (!_$createForm.valid()) {
    //         return;
    //     }

    //     var quotation = _$createForm.serializeFormToObject();

    //     abp.ui.setBusy(_$createModal);
    //     _quotationService.create(quotation).done(function () {
    //         _$createModal.modal("hide");
    //         _$createForm[0].reset();
    //         abp.notify.info(l("SavedSuccessfully"));
    //         _$quotationTable.ajax.reload();
    //     }).always(function () {
    //         abp.ui.clearBusy(_$createModal);
    //     });
    // });


    // 7. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$subscriptionTable.ajax.reload();
    });

    _$searchForm.find(".txt-search, input[type='number']").on("keypress", function (e) {
        if (e.which === 13) {
            _$subscriptionTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find("select, input[type='number']").on("change", function () {
        _$subscriptionTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$subscriptionTable.ajax.reload();
    });

    // 8. MODAL EVENTS & ABP EVENT LISTENERS
    // _$createModal.on("shown.bs.modal", function () {
    //     _$createModal.find("input:not([type=hidden]):first").focus();
    // }).on("hidden.bs.modal", function () {
    //     _$createForm[0].reset();
    //     _$createForm.validate().resetForm();
    // });
})(jQuery);
