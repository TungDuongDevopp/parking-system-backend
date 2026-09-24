(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _staffService = abp.services.app.staff,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS
    var _$createModal = $("#StaffCreateModal"),
        _$createForm = _$createModal.find("form"),
        _$table = $("#StaffTable"),
        _$searchForm = $("#StaffSearchForm"),
        _$editModal = $("#StaffEditModal"),
        _$statusModal = $("#StaffStatusModal"),
        _$statusForm = $("#StaffStatusForm");

    // 3. PERMISSIONS (UX only)
    var isManager = abp.auth.isGranted("Pages.Staffs.Manager"),
        canEdit = abp.auth.isGranted("Pages.Staffs"),
        canDelete = isManager;

    // 4. DATATABLE INITIALIZATION
    var _$staffTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _staffService.getAll,
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
                    _$staffTable.draw(false);
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
            { targets: 1, data: "name" },
            { targets: 2, data: "phoneNumber" },
            { targets: 3, data: "email" },
            {
                targets: 4, data: "genderName",
                name: "gender"
            },
            {
                targets: 5,
                data: "dateOfBirth",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD") : "";
                }
            },
            {
                targets: 6,
                data: "hiredDate",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD") : "";
                }
            },
            {
                targets: 7,
                data: "status",
                render: function (data, type, row) {
                    if (data === 0) {
                        return '<span class="badge badge-success bg-success">' + l("Active") + '</span>';
                    } else if (data === 1) {
                        return '<span class="badge badge-secondary bg-secondary">' + l("Inactive") + '</span>';
                    } else if (data === 2) {
                        return '<span class="badge badge-danger bg-danger">' + l("Suspended") + '</span>';
                    }
                    return row.statusName || "";
                }
            },
            {
                targets: 8,
                data: null,
                orderable: false,
                autoWidth: false,
                defaultContent: "",
                render: function (data, type, row) {
                    var actions = [];

                    if (canEdit) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-secondary edit-staff me-1 mr-1" data-id="' + row.id + '" data-bs-toggle="modal" data-bs-target="#StaffEditModal">',
                            '    <i class="fas fa-pencil-alt"></i> ' + l("Edit"),
                            '</button>'
                        );
                    }

                    if (isManager) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-info change-status-staff me-1 mr-1" data-id="' + row.id + '" data-name="' + (row.name || "") + '" data-status="' + row.status + '" data-bs-toggle="modal" data-bs-target="#StaffStatusModal">',
                            '    <i class="fas fa-toggle-on"></i> ' + l("ChangeStatus"),
                            '</button>'
                        );
                    }

                    if (canDelete) {
                        actions.push(
                            '<button type="button" class="btn btn-sm bg-danger delete-staff" data-id="' + row.id + '" data-name="' + (row.name || "") + '">',
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
            UserId: {
                required: true
            },
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

    // 6. CREATE (SAVE)
    _$createForm.find(".save-button").on("click", function (e) {
        e.preventDefault();
        if (!_$createForm.valid()) {
            return;
        }

        var staff = _$createForm.serializeFormToObject();
        if (staff.Gender === "true") staff.Gender = true;
        else if (staff.Gender === "false") staff.Gender = false;
        else staff.Gender = null;

        abp.ui.setBusy(_$createModal);
        _staffService.create(staff).done(function () {
            _$createModal.modal("hide");
            _$createForm[0].reset();
            abp.notify.info(l("SavedSuccessfully"));
            _$staffTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$createModal);
        });
    });

    // 7. EDIT MODAL OPEN
    $(document).on("click", ".edit-staff", function (e) {
        var id = $(this).attr("data-id");
        e.preventDefault();
        abp.ajax({
            url: abp.appPath + "Staff/EditModal?staffId=" + id,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$editModal.find("div.modal-content").html(content);
            }
        });
    });

    // 8. DELETE
    $(document).on("click", ".delete-staff", function () {
        var id = $(this).attr("data-id");
        var name = $(this).attr("data-name");

        deleteStaff(id, name);
    });

    function deleteStaff(id, name) {
        abp.message.confirm(
            abp.utils.formatString(l("AreYouSureWantToDelete"), name),
            null,
            function (isConfirmed) {
                if (isConfirmed) {
                    _staffService.delete({ id: id }).done(function () {
                        abp.notify.info(l("SuccessfullyDeleted"));
                        _$staffTable.ajax.reload();
                    });
                }
            }
        );
    }

    // 9. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$staffTable.ajax.reload();
    });

    _$searchForm.find(".txt-search").on("keypress", function (e) {
        if (e.which === 13) {
            _$staffTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find(".staff-status-filter, input[type='date']").on("change", function () {
        _$staffTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$staffTable.ajax.reload();
    });

    // 10. MODAL EVENTS & ABP EVENT LISTENERS
    _$createModal.on("shown.bs.modal", function () {
        _$createModal.find("input:not([type=hidden]):first").focus();
    }).on("hidden.bs.modal", function () {
        _$createForm[0].reset();
        _$createForm.validate().resetForm();
    });

    abp.event.on("staff.edited", function () {
        _$staffTable.ajax.reload();
    });

    // --- DOMAIN-SPECIFIC BUSINESS FEATURES (STAFF) ---
    // Auto fill Name, Email, Phone when User selected in Create Modal
    $("#StaffCreate_UserId").on("change", function () {
        var $opt = $(this).find("option:selected");
        var name = $opt.data("name");
        var email = $opt.data("email");
        var phone = $opt.data("phone");

        if (name && !$("#StaffCreate_Name").val()) {
            $("#StaffCreate_Name").val(name);
        }
        if (email && !$("#StaffCreate_Email").val()) {
            $("#StaffCreate_Email").val(email);
        }
        if (phone && !$("#StaffCreate_Phone").val()) {
            $("#StaffCreate_Phone").val(phone);
        }
    });

    // Change Status Modal Open
    $(document).on("click", ".change-status-staff", function () {
        var staffId = $(this).attr("data-id");
        var staffName = $(this).attr("data-name");
        var status = $(this).attr("data-status");

        $("#StaffStatus_Id").val(staffId);
        $("#StaffStatus_Name").text(staffName);
        $("#StaffStatus_Select").val(status);
    });

    // Change Status Form Submit
    _$statusForm.on("submit", function (e) {
        e.preventDefault();
        var staffId = $("#StaffStatus_Id").val();
        var newStatus = parseInt($("#StaffStatus_Select").val(), 10);

        abp.ui.setBusy(_$statusModal);
        _staffService.changeStatus({
            id: staffId,
            status: newStatus
        }).done(function () {
            _$statusModal.modal("hide");
            abp.notify.info(l("SavedSuccessfully"));
            _$staffTable.ajax.reload();
        }).always(function () {
            abp.ui.clearBusy(_$statusModal);
        });
    });

})(jQuery);
