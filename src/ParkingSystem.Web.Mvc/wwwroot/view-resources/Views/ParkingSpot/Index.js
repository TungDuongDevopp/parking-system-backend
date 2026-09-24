(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _parkingSpotService = abp.services.app.parkingSpot,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#ParkingSpotCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#ParkingSpotTable"),
        _$searchForm = $("#ParkingSpotSearchForm"),
        _$statusModal = $("#ParkingSpotStatusModal"),
         _$editModal = $("#ParkingSpotEditModal"),
        _$statusForm = $("#ParkingSpotStatusForm");

    // 3. PERMISSIONS (UX only)
    var isManager = abp.auth.isGranted("Pages.ParkingSpots.Manager"),
        canCreate = isManager,
        canDelete = isManager,
        canEdit = isManager;

    // 4. DATATABLE INITIALIZATION
    var _$parkingSpotTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _parkingSpotService.getAll,
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
                    _$parkingSpotTable.draw(false);
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
            { targets: 1, data: "spotCode" },
            { targets: 2, data: "parkingAreaId" },
            { targets: 3, data: "parkingAreaCode" },
            { targets: 4, data: "parkingAreaName" },
            {
                targets: 5,
                data: "status",
                render: function (data) {
                    var statusMap = {
                        1: { cls: "bg-success", label: "Available" },
                        2: { cls: "bg-warning", label: "Reserved" },
                        3: { cls: "bg-danger",  label: "Occupied" },
                        4: { cls: "bg-secondary", label: "Unavailable" }
                    };
                    var s = statusMap[data] || { cls: "bg-light", label: data };
                    return '<span class="badge ' + s.cls + '">' + l(s.label) + '</span>';
                }
            },
            {
                targets: 6,
                data: "creationTime",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            {
                targets: 7,
                data: null,
                orderable: false,
                autoWidth: false,
                defaultContent: "",
                render: function (data, type, row) {
                    var actions = [];
                      if (canEdit) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-secondary edit-parking-spot me-1 mr-1" data-id="' + row.id + '" data-bs-toggle="modal" data-bs-target="#ParkingSpotEditModal">',
                            '    <i class="fas fa-pencil-alt"></i> ' + l("Edit"),
                            '</button>'
                        );
                    }

                    // ChangeStatus chỉ khi Spot không đang Reserved(2) hoặc Occupied(3)
                    if (isManager && row.status !== 2 && row.status !== 3) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-info change-status-parking-spot me-1 mr-1"' +
                            ' data-id="' + row.id + '"' +
                            ' data-code="' + (row.spotCode || "") + '"' +
                            ' data-status="' + row.status + '"' +
                            ' data-bs-toggle="modal" data-bs-target="#ParkingSpotStatusModal">',
                            '    <i class="fas fa-toggle-on"></i> ' + l("ChangeStatus"),
                            '</button>'
                        );
                    }

                    if (canDelete) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-danger delete-parking-spot"' +
                            ' data-id="' + row.id + '"' +
                            ' data-code="' + (row.spotCode || "") + '">',
                            '    <i class="fas fa-trash"></i> ' + l("Delete"),
                            '</button>'
                        );
                    }

                    return actions.join("");
                }
            }
        ]
    });

    // 5. FORM VALIDATION (CREATE)
    _$createForm.validate({
        rules: {
            SpotCode: {
                required: true,
                maxlength: 30
            },
            ParkingAreaId: {
                required: true,
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

        var spot = _$createForm.serializeFormToObject();

        abp.ui.setBusy(_$createModal);
        _parkingSpotService.create(spot).done(function () {
            _$createModal.modal("hide");
            _$createForm[0].reset();
            abp.notify.info(l("SavedSuccessfully"));
            _$parkingSpotTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$createModal);
        });
    });

    // 7. DELETE
    $(document).on("click", ".delete-parking-spot", function () {
        var spotId = $(this).attr("data-id");
        var spotCode = $(this).attr("data-code");

        abp.message.confirm(
            abp.utils.formatString(l("ParkingSpotDeleteWarningMessage"), spotCode),
            function (isConfirmed) {
                if (isConfirmed) {
                    _parkingSpotService.delete({ id: spotId }).done(function () {
                        abp.notify.info(l("SuccessfullyDeleted"));
                        _$parkingSpotTable.ajax.reload();
                    });
                }
            }
        );
    });
       // 8. EDIT MODAL OPEN
    $(document).on("click", ".edit-parking-spot", function (e) {
        var id = $(this).attr("data-id");
        e.preventDefault();
        abp.ajax({
            url: abp.appPath + "ParkingSpot/EditModal?parkingSpotId=" + id,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$editModal.find("div.modal-content").html(content);
            }
        });
    });

    // 9. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$parkingSpotTable.ajax.reload();
    });

    _$searchForm.find(".txt-search, input[type='number']").on("keypress", function (e) {
        if (e.which === 13) {
            _$parkingSpotTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find("select, input[type='number']").on("change", function () {
        _$parkingSpotTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$parkingSpotTable.ajax.reload();
    });

    // 10. CREATE MODAL EVENTS
    _$createModal.on("shown.bs.modal", function () {
        _$createModal.find("input:not([type=hidden]):first").focus();
    }).on("hidden.bs.modal", function () {
        _$createForm[0].reset();
        _$createForm.validate().resetForm();
    });

    // 11. CHANGE STATUS - Open modal
    $(document).on("click", ".change-status-parking-spot", function () {
        var spotId = $(this).attr("data-id");
        var spotCode = $(this).attr("data-code");
        var status = $(this).attr("data-status");

        $("#ParkingSpotStatus_Id").val(spotId);
        $("#ParkingSpotStatus_Code").text(spotCode);
        $("#ParkingSpotStatus_Select").val(status);
    });

    // 12. CHANGE STATUS - Submit
    _$statusForm.on("submit", function (e) {
        e.preventDefault();
        var spotId = $("#ParkingSpotStatus_Id").val();
        var newStatus = parseInt($("#ParkingSpotStatus_Select").val(), 10);

        abp.ui.setBusy(_$statusModal);
        _parkingSpotService.changeStatus({
            id: spotId,
            status: newStatus
        }).done(function () {
            _$statusModal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            _$parkingSpotTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$statusModal);
        });
    });

})(jQuery);
