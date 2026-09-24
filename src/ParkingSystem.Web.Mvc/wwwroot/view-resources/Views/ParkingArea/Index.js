(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _parkingAreaService = abp.services.app.parkingArea,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#ParkingAreaCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#ParkingAreaTable"),
        _$searchForm = $("#ParkingAreaSearchForm"),
        _$editModal = $("#ParkingAreaEditModal");

    // 3. PERMISSIONS (UX only)
    var canEdit = abp.auth.isGranted("Pages.ParkingAreas"),
        canDelete = canEdit;

    // 4. DATATABLE INITIALIZATION
    var _$parkingAreaTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _parkingAreaService.getAll,
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
                    _$parkingAreaTable.draw(false);
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
            { targets: 1, data: "parkingCode" },
            { targets: 2, data: "name" },
            { targets: 3, data: "vehicleTypeName" },
            { targets: 4, data: "capacity" },
            { targets: 5, data: "parkingModeName" },
            { targets: 6, data: "location" },
            {
                targets: 7,
                data: "parkingAreaStatusName", name: "status",
                render: function (data, type, row) {
                    var isAct = (row.status === 1 || data === "Active");
                    var badgeClass = isAct ? "badge-success bg-success" : "badge-secondary bg-secondary";
                    return '<span class="badge ' + badgeClass + '">' + (data || "") + '</span>';
                }
            },
            {
                targets: 8,
                data: "creationTime",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            {
                targets: 9,
                data: null,
                orderable: false,
                autoWidth: false,
                defaultContent: "",
                render: function (data, type, row) {
                    var actions = [];

                    if (canEdit) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-secondary edit-parking-area me-1 mr-1" data-id="' + row.id + '" data-bs-toggle="modal" data-bs-target="#ParkingAreaEditModal">',
                            '    <i class="fas fa-pencil-alt"></i> ' + l("Edit"),
                            '</button>'
                        );
                    }

                    if (canDelete) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-danger delete-parking-area" data-id="' + row.id + '" data-name="' + (row.name || "") + '">',
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

    // 6. CREATE (SAVE)
    _$createForm.find(".save-button").on("click", function (e) {
        e.preventDefault();
        if (!_$createForm.valid()) {
            return;
        }

        var parkingArea = _$createForm.serializeFormToObject();

        abp.ui.setBusy(_$createModal);
        _parkingAreaService.create(parkingArea).done(function () {
            _$createModal.modal("hide");
            _$createForm[0].reset();
            abp.notify.info(l("SavedSuccessfully"));
            _$parkingAreaTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$createModal);
        });
    });

    // 7. EDIT MODAL OPEN
    $(document).on("click", ".edit-parking-area", function (e) {
        var id = $(this).attr("data-id");
        e.preventDefault();
        abp.ajax({
            url: abp.appPath + "ParkingArea/EditModal?parkingAreaId=" + id,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$editModal.find("div.modal-content").html(content);
            }
        });
    });

    // 8. DELETE
    $(document).on("click", ".delete-parking-area", function () {
        var id = $(this).attr("data-id");
        var name = $(this).attr("data-name");

        deleteParkingArea(id, name);
    });

    function deleteParkingArea(id, name) {
        abp.message.confirm(
            abp.utils.formatString(l("AreYouSureWantToDelete"), name),
            null,
            function (isConfirmed) {
                if (isConfirmed) {
                    _parkingAreaService.delete({ id: id }).done(function () {
                        abp.notify.info(l("SuccessfullyDeleted"));
                        _$parkingAreaTable.ajax.reload();
                    });
                }
            }
        );
    }

    // 9. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$parkingAreaTable.ajax.reload();
    });

    _$searchForm.find(".txt-search, input[type='number']").on("keypress", function (e) {
        if (e.which === 13) {
            _$parkingAreaTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find("select, input[type='number']").on("change", function () {
        _$parkingAreaTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$parkingAreaTable.ajax.reload();
    });

    // 10. MODAL EVENTS & ABP EVENT LISTENERS
    _$createModal.on("shown.bs.modal", function () {
        _$createModal.find("input:not([type=hidden]):first").focus();
    }).on("hidden.bs.modal", function () {
        _$createForm[0].reset();
        _$createForm.validate().resetForm();
    });

    abp.event.on("parkingArea.edited", function () {
        _$parkingAreaTable.ajax.reload();
    });

})(jQuery);
