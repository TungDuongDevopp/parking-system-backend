(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _vehicleService = abp.services.app.vehicle,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#VehicleCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#VehicleTable"),
        _$searchForm = $("#VehicleSearchForm"),
        _$editModal = $("#VehicleEditModal");

    // 3. PERMISSIONS (UX only)
    var canEdit = abp.auth.isGranted("Pages.Vehicles"),
        canDelete = canEdit;

    // 4. DATATABLE INITIALIZATION
    var _$vehicleTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _vehicleService.getAll,
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
                    _$vehicleTable.draw(false);
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
            { targets: 1, data: "vehicleTypeName" ,name:"vehicleType" },
            { targets: 2, data: "licensePlate" },
            { targets: 3, data: "brand" },
            { targets: 4, data: "color" },
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
                            '<button type="button" class="btn btn-sm bg-secondary edit-vehicle me-1 mr-1" data-id="' + row.id + '" data-bs-toggle="modal" data-bs-target="#VehicleEditModal">',
                            '    <i class="fas fa-pencil-alt"></i> ' + l("Edit"),
                            '</button>'
                        );
                    }

                    if (canDelete) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-danger delete-vehicle" data-id="' + row.id + '" data-name="' + (row.licensePlate || row.brand || "") + '">',
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
                required: true
            },
            LicensePlate: {
                maxlength: 30
            },
            Brand: {
                maxlength: 255
            },
            Color: {
                required: true,
                maxlength: 255
            }
        }
    });

    // 6. CREATE (SAVE)
    _$createForm.find(".save-button").on("click", function (e) {
        e.preventDefault();
        if (!_$createForm.valid()) {
            return;
        }

        var vehicle = _$createForm.serializeFormToObject();

        abp.ui.setBusy(_$createModal);
        _vehicleService.create(vehicle).done(function () {
            _$createModal.modal("hide");
            _$createForm[0].reset();
            abp.notify.info(l("SavedSuccessfully"));
            _$vehicleTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$createModal);
        });
    });

    // 7. EDIT MODAL OPEN
    $(document).on("click", ".edit-vehicle", function (e) {
        var id = $(this).attr("data-id");
        e.preventDefault();
        abp.ajax({
            url: abp.appPath + "Vehicle/EditModal?vehicleId=" + id,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$editModal.find("div.modal-content").html(content);
            }
        });
    });

    // 8. DELETE
    $(document).on("click", ".delete-vehicle", function () {
        var id = $(this).attr("data-id");
        var name = $(this).attr("data-name");

        deleteVehicle(id, name);
    });

    function deleteVehicle(id, name) {
        abp.message.confirm(
            abp.utils.formatString(l("AreYouSureWantToDelete"), name),
            null,
            function (isConfirmed) {
                if (isConfirmed) {
                    _vehicleService.delete({ id: id }).done(function () {
                        abp.notify.info(l("SuccessfullyDeleted"));
                        _$vehicleTable.ajax.reload();
                    });
                }
            }
        );
    }

    // 9. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$vehicleTable.ajax.reload();
    });

    _$searchForm.find(".txt-search").on("keypress", function (e) {
        if (e.which === 13) {
            _$vehicleTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find(".vehicle-type-filter").on("change", function () {
        _$vehicleTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$vehicleTable.ajax.reload();
    });

    // 10. MODAL EVENTS & ABP EVENT LISTENERS
    _$createModal.on("shown.bs.modal", function () {
        _$createModal.find("input:not([type=hidden]):first").focus();
    }).on("hidden.bs.modal", function () {
        _$createForm[0].reset();
        _$createForm.validate().resetForm();
    });

    abp.event.on("vehicle.edited", function () {
        _$vehicleTable.ajax.reload();
    });

})(jQuery);
