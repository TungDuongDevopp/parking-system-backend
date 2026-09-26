(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _quotationService = abp.services.app.quotation,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#QuotationCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#QuotationTable"),
        _$searchForm = $("#QuotationSearchForm"),
        _$editModal = $("#QuotationEditModal");
      
    // 3. PERMISSIONS (UX only)
    var isManager = abp.auth.isGranted("Pages.Quotations.Manager"),
        canEdit = isManager,
        canDelete = isManager;

    // 4. DATATABLE INITIALIZATION
    var _$quotationTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _quotationService.getAll,
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
                    _$quotationTable.draw(false);
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
            { targets: 1, data: "vehicleTypeName", name:"vehicleType" },
            { targets: 2, data: "duration" },
            { targets: 3, data: "durationUnitType",name:"durationUnit" },
            { targets: 4, data: "price" },
            {
                targets: 5,
                data: "creationTime",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            {
                targets: 6,
                data: null,
                orderable: false,
                autoWidth: false,
                defaultContent: "",
                render: function (data, type, row) {
                    var actions = [];

                    if (canEdit) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-secondary edit-quotation me-1 mr-1" data-id="' + row.id + '" data-bs-toggle="modal" data-bs-target="#QuotationEditModal">',
                            '    <i class="fas fa-pencil-alt"></i> ' + l("Edit"),
                            '</button>'
                        );
                    }
                    if (canDelete) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-danger delete-quotation" data-id="' + row.id + '" data-vehicle-type-name="' + (row.vehicleTypeName || "") + '">',
                            '    <i class="fas fa-trash"></i> ' + l("Delete"),
                            '</button>'
                        );
                    }
                    return actions.join("");
                }
            }
        ]
    });

    // 5. FORM VALIDATION (CREATE) - Validated entirely in JS
    _$createForm.validate({
        rules: {
            VehicleType: {
                required: true,   
            },
            DurationUnit: {
                required: true,  
            },
            Duration: {
                required: true,
                number: true,
                min: 1
            },
            Price: {
                required: true,
                number: true,
                min: 1
            }
        }
    });

    // 6. CREATE (SAVE)
    _$createForm.find(".save-button").on("click", function (e) {
        e.preventDefault();
        if (!_$createForm.valid()) {
            return;
        }

        var quotation = _$createForm.serializeFormToObject();

        abp.ui.setBusy(_$createModal);
        _quotationService.create(quotation).done(function () {
            _$createModal.modal("hide");
            _$createForm[0].reset();
            abp.notify.info(l("SavedSuccessfully"));
            _$quotationTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$createModal);
        });
    });
    // 7. EDIT MODAL OPEN
    $(document).on("click", ".edit-quotation", function (e) {
        var id = $(this).attr("data-id");
        e.preventDefault();

        abp.ajax({
            url: abp.appPath + "Quotation/EditModal?quotationId=" + id,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$editModal.find("div.modal-content").html(content);
            }
        });
    });

    // 8. DELETE
    $(document).on("click", ".delete-quotation", function () {
        var id = $(this).attr("data-id");
        var vehicleType = $(this).attr("data-vehicle-type-name");

        deleteQuotation(id, vehicleType);
    });

    function deleteQuotation(id, vehicleType) {
        abp.message.confirm(
            abp.utils.formatString(l("AreYouSureWantToDelete"), vehicleType),
            null,
            function (isConfirmed) {
                if (isConfirmed) {
                    _quotationService.delete({ id: id }).done(function () {
                        abp.notify.info(l("SuccessfullyDeleted"));
                        _$quotationTable.ajax.reload();
                    });
                }
            }
        );
    }

    // 9. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$quotationTable.ajax.reload();
    });

    _$searchForm.find(".txt-search, input[type='number']").on("keypress", function (e) {
        if (e.which === 13) {
            _$quotationTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find("select, input[type='number']").on("change", function () {
        _$quotationTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$quotationTable.ajax.reload();
    });

    // 10. MODAL EVENTS & ABP EVENT LISTENERS
    _$createModal.on("shown.bs.modal", function () {
        _$createModal.find("input:not([type=hidden]):first").focus();
    }).on("hidden.bs.modal", function () {
        _$createForm[0].reset();
        _$createForm.validate().resetForm();
    });

    abp.event.on("quotation.edited", function () {
        _$quotationTable.ajax.reload();
    });
})(jQuery);
