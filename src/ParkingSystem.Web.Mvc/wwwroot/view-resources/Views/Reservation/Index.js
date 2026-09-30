(function ($) {
    // 1. SERVICES & LOCALIZATION
    var _reservationService = abp.services.app.reservation,
        l = abp.localization.getSource("ParkingSystem");

    // 2. DOM ELEMENTS 
        _$table = $("#ReservationTable"),
        _$searchForm = $("#ReservationSearchForm");

    // 3. DATATABLE INITIALIZATION
    var _$reservationTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _reservationService.getAll,
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
                    _$reservationTable.draw(false);
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
            { targets: 1, data: "customerName", name: "customerName" },
            { targets: 2, data: "parkingAreaCode" },
            { targets: 3, data: "parkingAreaName" },
            { targets: 4, data: "parkingSpotCode" },
            {
                targets: 5, data: "reservedAt",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            {
                targets: 6, data: "endTime",
                render: function (data) {
                    return data ? moment(data).format("YYYY-MM-DD HH:mm:ss") : "";
                }
            },
            {
                targets: 7, data: "status", name: "status",
                render: function (data, type, row) {
                    if (data === 1) {
                        return '<span class="badge badge-info bg-info">'
                            + l("Reserved") + '</span>';
                    }

                    if (data === 2) {
                        return '<span class="badge badge-success bg-success">'
                            + l("Completed") + '</span>';
                    }

                    if (data === 3) {
                        return '<span class="badge badge-secondary bg-secondary">'
                            + l("Canceled") + '</span>';
                    }

                    if (data === 4) {
                        return '<span class="badge badge-warning bg-warning text-dark">'
                            + l("Expired") + '</span>';
                    }

                    if (data === 5) {
                        return '<span class="badge badge-danger bg-danger">'
                            + l("Invalid") + '</span>';
                    }
                    return row.statusName || "";
                }

            }
        ]
    });



    // 4. SEARCH & FILTERS
    _$searchForm.find(".btn-search").on("click", function () {
        _$reservationTable.ajax.reload();
    });

    _$searchForm.find(".txt-search, input[type='number']").on("keypress", function (e) {
        if (e.which === 13) {
            _$reservationTable.ajax.reload();
            return false;
        }
    });

    _$searchForm.find("select, input[type='number']").on("change", function () {
        _$reservationTable.ajax.reload();
    });

    _$searchForm.find(".btn-clear-search").on("click", function () {
        _$searchForm[0].reset();
        _$reservationTable.ajax.reload();
    });

})(jQuery);
