(function ($) {
    'use strict';

    // ─── 1. SERVICES & LOCALIZATION ─────────────────────────────────────────────
    var _reservationService = abp.services.app.reservation,
        _parkingAreaService = abp.services.app.parkingArea,
        _parkingSpotService = abp.services.app.parkingSpot,
        _vehicleService     = abp.services.app.vehicle,
        _subscriptionService= abp.services.app.subscription,
        l                   = abp.localization.getSource('ParkingSystem');

    // ─── 2. DOM REFERENCES ──────────────────────────────────────────────────────
    var $subAlertBox          = $('#subscription-alert-box'),
        $activeLoading        = $('#active-reservation-loading'),
        $activeEmpty          = $('#active-reservation-empty'),
        $activeCard           = $('#active-reservation-card'),
        $activeStatusBadge    = $('#active-status-badge'),
        $activeAreaCode       = $('#active-area-code'),
        $activeAreaName       = $('#active-area-name'),
        $activeSpotCode       = $('#active-spot-code'),
        $activeSpotDesc       = $('#active-spot-desc'),
        $activeVehicleType    = $('#active-vehicle-type'),
        $activeVehicleIcon    = $('#active-vehicle-icon'),
        $activeReservedAt     = $('#active-reserved-at'),
        $activeEndTime        = $('#active-end-time'),
        $btnCancelActive      = $('#btn-cancel-active-reservation'),
        $historyTbody         = $('#history-tbody'),
        $historyEmpty         = $('#history-empty'),
        $historyStatusFilter  = $('#history-status-filter'),
        $btnRefreshHistory    = $('#btn-refresh-history'),
        $modal                = $('#CreateReservationModal'),
        $modalSubInfo         = $('#modal-sub-info'),
        $modalSubText         = $('#modal-sub-text'),
        $btnOpenModal         = $('#btn-open-create-modal'),
        $vehicleSelect        = $('#res-vehicle-select'),
        $areaSelect           = $('#res-area-select'),
        $areaBadge            = $('#area-details-badge'),
        $spotContainer        = $('#spot-selection-container'),
        $spotSelect           = $('#res-spot-select'),
        $spotLoading          = $('#spot-loading'),
        $noSpotsAlert         = $('#no-spots-alert'),
        $capacityNotice       = $('#capacity-mode-notice'),
        $reservedAtInput      = $('#res-reserved-at'),
        $summaryCard          = $('#reservation-summary'),
        $summaryArea          = $('#summary-area-code'),
        $summarySpot          = $('#summary-spot-code'),
        $summaryStart         = $('#summary-start-time'),
        $summaryExpire        = $('#summary-expire-time'),
        $btnSubmit            = $('#btn-submit-reservation');

    // ─── 3. STATE ────────────────────────────────────────────────────────────────
    var _activeReservation    = null,
        _currentSubscription  = null,
        _customerVehicles     = [],
        _availableAreas       = [],
        _cachedHistoryItems   = [];

    // ─── 4. STATUS CONFIG & HELPERS ──────────────────────────────────────────────
    var statusConfig = {
        1: { label: l('Reserved'),  cls: 'badge-info bg-info text-white' },
        2: { label: l('Completed'), cls: 'badge-success bg-success text-white' },
        3: { label: l('Cancelled'), cls: 'badge-secondary bg-secondary text-white' },
        4: { label: l('Expired'),   cls: 'badge-warning bg-warning text-dark' },
        5: { label: l('Invalid'),   cls: 'badge-danger bg-danger text-white' }
    };

    function statusBadge(status) {
        var cfg = statusConfig[status] || { label: status, cls: 'badge-light' };
        return '<span class="badge ' + cfg.cls + ' px-2 py-1" style="font-size:.85rem;">' + cfg.label + '</span>';
    }

    var vehicleIcons = {
        1: 'fa-bicycle',
        2: 'fa-bicycle',
        3: 'fa-motorcycle',
        4: 'fa-motorcycle',
        5: 'fa-car',
        'Bicycle': 'fa-bicycle',
        'ElectricBicycle': 'fa-bicycle',
        'Motorcycle': 'fa-motorcycle',
        'ElectricMotorcycle': 'fa-motorcycle',
        'Car': 'fa-car'
    };

    function vehicleIcon(typeNameOrVal) {
        return vehicleIcons[typeNameOrVal] || 'fa-car';
    }

    function fmtDateTime(val) {
        return val ? moment(val).format('YYYY-MM-DD HH:mm') : '—';
    }

    // ─── 5. SUBSCRIPTION VERIFICATION ───────────────────────────────────────────
    function checkSubscription() {
        return _subscriptionService.getMyCurrentSubscription()
            .done(function (sub) {
                _currentSubscription = sub;
                if (!sub || sub.status !== 1) {
                    $subAlertBox.show();
                    $modalSubInfo.hide();
                } else {
                    $subAlertBox.hide();
                    $modalSubText.text('Active Plan: ' + (sub.vehicleTypeName || sub.vehicleType) + ' (' + sub.duration + ' ' + (sub.durationUnitName || '') + ')');
                    $modalSubInfo.show();
                }
            })
            .fail(function () {
                $subAlertBox.show();
            });
    }

    // ─── 6. LOAD ACTIVE RESERVATION & HISTORY ────────────────────────────────────
    function loadReservations() {
        $activeLoading.show();
        $activeEmpty.hide();
        $activeCard.hide();

        _reservationService.getAll({ maxResultCount: 50, skipCount: 0 })
            .done(function (result) {
                $activeLoading.hide();

                var items = result.items || [];
                _cachedHistoryItems = items;

                // Find active reservation: Status == 1 (Reserved) and endTime > now
                var now = moment();
                _activeReservation = null;

                for (var i = 0; i < items.length; i++) {
                    var item = items[i];
                    if (item.status === 1) {
                        var expire = item.endTime ? moment(item.endTime) : moment(item.reservedAt).add(3, 'hours').add(15, 'minutes');
                        if (expire.isAfter(now)) {
                            _activeReservation = item;
                            break;
                        }
                    }
                }

                if (_activeReservation) {
                    renderActiveReservation(_activeReservation);
                } else {
                    $activeEmpty.show();
                }

                renderHistoryTable();
            })
            .fail(function () {
                $activeLoading.hide();
                $activeEmpty.show();
                renderHistoryTable();
            });
    }

    function renderActiveReservation(res) {
        $activeAreaCode.text(res.parkingAreaCode || '—');
        $activeAreaName.text(res.parkingAreaName || '');

        if (res.parkingSpotCode) {
            $activeSpotCode.text(res.parkingSpotCode);
            $activeSpotDesc.text('Designated spot');
        } else {
            $activeSpotCode.text('Auto-assigned');
            $activeSpotDesc.text('Capacity-based area');
        }

        var vTypeName = res.vehicleTypeName || (res.vehicleType != null ? res.vehicleType : 'Vehicle');
        $activeVehicleType.text(vTypeName);
        var icon = vehicleIcon(res.vehicleType || vTypeName);
        $activeVehicleIcon.attr('class', 'fas ' + icon);

        $activeReservedAt.text(fmtDateTime(res.reservedAt));
        $activeEndTime.text('Valid until: ' + fmtDateTime(res.endTime));

        $activeStatusBadge.html(statusBadge(res.status));
        $btnCancelActive.data('id', res.id);

        $activeCard.show();
    }

    function renderHistoryTable() {
        $historyTbody.empty();

        var filterStatus = $historyStatusFilter.val();
        var items = _cachedHistoryItems;

        if (filterStatus) {
            items = items.filter(function (x) {
                return String(x.status) === String(filterStatus);
            });
        }

        if (items.length === 0) {
            $historyEmpty.show();
            return;
        }

        $historyEmpty.hide();

        $.each(items, function (idx, item) {
            var tr = $('<tr>');
            tr.append($('<td>').text(idx + 1));
            tr.append($('<td>').html('<strong>' + (item.parkingAreaCode || '—') + '</strong>'));
            tr.append($('<td>').text(item.parkingAreaName || '—'));

            if (item.parkingSpotCode) {
                tr.append($('<td>').html('<span class="badge badge-light border">' + item.parkingSpotCode + '</span>'));
            } else {
                tr.append($('<td>').html('<span class="badge badge-secondary bg-secondary">Capacity-based</span>'));
            }

            var vTypeName = item.vehicleTypeName || (item.vehicleType != null ? item.vehicleType : '—');
            var vIcon = vehicleIcon(item.vehicleType || vTypeName);
            tr.append($('<td>').html('<i class="fas ' + vIcon + ' mr-1 me-1 text-muted"></i>' + vTypeName));

            tr.append($('<td>').text(fmtDateTime(item.reservedAt)));
            tr.append($('<td>').text(fmtDateTime(item.endTime)));
            tr.append($('<td>').html(statusBadge(item.status)));

            // Action
            var actionTd = $('<td class="text-center">');
            if (item.status === 1) {
                var cancelBtn = $(
                    '<button type="button" class="btn btn-outline-danger btn-xs btn-sm btn-cancel-history" data-id="' + item.id + '">' +
                    '  <i class="fas fa-times mr-1 me-1"></i>' + l('Cancel') +
                    '</button>'
                );
                actionTd.append(cancelBtn);
            } else {
                actionTd.text('—');
            }
            tr.append(actionTd);

            $historyTbody.append(tr);
        });
    }

    // ─── 7. LOAD MODAL LOOKUPS (Vehicles & Parking Areas) ───────────────────────
    function initModalFields() {
        // Datetime constraints: Must be today and >= now
        var nowMoment = moment();
        var defaultTime = moment().add(5, 'minutes').format('YYYY-MM-DDTHH:mm');
        var minTime     = nowMoment.format('YYYY-MM-DDTHH:mm');
        var maxTime     = moment().endOf('day').format('YYYY-MM-DDTHH:mm');

        $reservedAtInput.val(defaultTime).attr('min', minTime).attr('max', maxTime);

        // Reset selects & dynamic sections
        $vehicleSelect.empty().append($('<option value="">').text('-- ' + l('SelectVehicle') + ' --'));
        $areaSelect.empty().append($('<option value="">').text('-- ' + l('SelectParkingArea') + ' --'));
        $spotSelect.empty().append($('<option value="">').text('-- ' + l('SelectParkingSpot') + ' --'));
        $areaBadge.hide().empty();
        $spotContainer.hide();
        $capacityNotice.hide();
        $noSpotsAlert.hide();
        $summaryCard.hide();

        // 1. Load Customer Vehicles
        _vehicleService.getAll({ maxResultCount: 50, skipCount: 0 })
            .done(function (res) {
                _customerVehicles = res.items || [];
                $.each(_customerVehicles, function (_, v) {
                    var display = (v.licensePlate ? '[' + v.licensePlate + '] ' : '') +
                                  (v.brand ? v.brand + ' ' : '') +
                                  (v.color ? '(' + v.color + ') ' : '') +
                                  '- ' + (v.vehicleTypeName || v.vehicleType);
                    var opt = $('<option>')
                        .val(v.id)
                        .text(display)
                        .data('vehicle-type', v.vehicleType);
                    $vehicleSelect.append(opt);
                });
            });

        // 2. Load Active Parking Areas
        _parkingAreaService.getAll({ status: 1, maxResultCount: 100, skipCount: 0 })
            .done(function (res) {
                _availableAreas = res.items || [];
                renderAreaOptions();
            });
    }

    function renderAreaOptions(filterVehicleType) {
        $areaSelect.empty().append($('<option value="">').text('-- ' + l('SelectParkingArea') + ' --'));

        $.each(_availableAreas, function (_, a) {
            // If filterVehicleType specified, mark or match
            var isCompatible = !filterVehicleType || a.vehicleType === filterVehicleType;
            var modeName = a.parkingMode === 0 ? 'Individual' : 'Capacity';
            var availableSpots = a.capacity != null && a.currentOccupancy != null ? (a.capacity - a.currentOccupancy) : '';
            var spotText = availableSpots !== '' ? ' (' + availableSpots + ' spots left)' : '';

            var text = '[' + a.parkingCode + '] ' + a.name + ' - ' + (a.vehicleTypeName || a.vehicleType) + ' - ' + modeName + spotText;

            var opt = $('<option>')
                .val(a.id)
                .text(text)
                .data('mode', a.parkingMode)
                .data('code', a.parkingCode)
                .data('name', a.name)
                .data('vehicle-type', a.vehicleType)
                .data('location', a.location || '')
                .data('capacity', a.capacity)
                .data('occupancy', a.currentOccupancy);

            if (filterVehicleType && !isCompatible) {
                opt.text(text + ' (Mismatch vehicle type)').addClass('text-muted');
            }

            $areaSelect.append(opt);
        });
    }

    // ─── 8. DEPENDENT SELECTIONS EVENT HANDLERS ─────────────────────────────────

    // When customer selects a vehicle: filter matching parking areas
    $vehicleSelect.on('change', function () {
        var selectedOpt = $(this).find('option:selected');
        var vType = selectedOpt.data('vehicle-type');

        renderAreaOptions(vType);

        // If previously selected area does not match, reset area
        $areaBadge.hide().empty();
        $spotContainer.hide();
        $capacityNotice.hide();
        $summaryCard.hide();
    });

    // When customer selects a parking area
    $areaSelect.on('change', function () {
        var areaId = $(this).val();
        if (!areaId) {
            $areaBadge.hide().empty();
            $spotContainer.hide();
            $capacityNotice.hide();
            $summaryCard.hide();
            return;
        }

        var opt = $(this).find('option:selected');
        var mode = opt.data('mode'); // 0 = individualSpot, 1 = capacityBase
        var code = opt.data('code');
        var name = opt.data('name');
        var location = opt.data('location');
        var capacity = opt.data('capacity');
        var occupancy = opt.data('occupancy');

        // Show area info badge
        var detailsHtml = '<span class="badge badge-primary bg-primary mr-1 me-1"><i class="fas fa-parking"></i> ' + code + '</span> ' +
                          '<span class="badge badge-info bg-info mr-1 me-1">' + (mode === 0 ? 'Individual Spots' : 'Capacity Base') + '</span> ' +
                          (location ? '<span class="badge badge-secondary bg-secondary mr-1 me-1"><i class="fas fa-map-marker-alt"></i> ' + location + '</span> ' : '') +
                          '<span class="badge badge-light border">Capacity: ' + (occupancy || 0) + '/' + (capacity || 0) + '</span>';
        $areaBadge.html(detailsHtml).show();

        if (mode === 1) {
            // Capacity-based mode: spots are auto-managed, SpotId must be null
            $spotContainer.hide();
            $spotSelect.empty().append($('<option value="">').text('-- Auto-assigned --'));
            $capacityNotice.show();
            updateSummary();
        } else {
            // Individual spot mode: customer must choose an available spot
            $capacityNotice.hide();
            $spotSelect.empty().append($('<option value="">').text('-- ' + l('SelectParkingSpot') + ' --'));
            $spotContainer.show();
            $spotLoading.show();
            $noSpotsAlert.hide();

            _parkingSpotService.getAll({ parkingAreaId: areaId, status: 1, maxResultCount: 100 })
                .done(function (res) {
                    $spotLoading.hide();
                    var spots = res.items || [];
                    if (spots.length === 0) {
                        $noSpotsAlert.show();
                    } else {
                        $noSpotsAlert.hide();
                        $.each(spots, function (_, s) {
                            var spotOpt = $('<option>')
                                .val(s.id)
                                .text(s.spotCode)
                                .data('spot-code', s.spotCode);
                            $spotSelect.append(spotOpt);
                        });
                    }
                    updateSummary();
                })
                .fail(function () {
                    $spotLoading.hide();
                    $noSpotsAlert.show();
                });
        }
    });

    $spotSelect.on('change', function () {
        updateSummary();
    });

    $reservedAtInput.on('change', function () {
        updateSummary();
    });

    function updateSummary() {
        var areaOpt = $areaSelect.find('option:selected');
        var areaId  = $areaSelect.val();

        if (!areaId) {
            $summaryCard.hide();
            return;
        }

        var areaCode = areaOpt.data('code') || '—';
        var mode     = areaOpt.data('mode');
        var spotCode = 'Auto-assigned (Capacity-based)';

        if (mode === 0) {
            var spotOpt = $spotSelect.find('option:selected');
            spotCode = spotOpt.data('spot-code') || 'Not selected';
        }

        var startTimeVal = $reservedAtInput.val();
        var startTimeStr = startTimeVal ? moment(startTimeVal).format('YYYY-MM-DD HH:mm') : '—';
        var expireTimeStr= startTimeVal ? moment(startTimeVal).add(3, 'hours').add(15, 'minutes').format('YYYY-MM-DD HH:mm') : '—';

        $summaryArea.text(areaCode);
        $summarySpot.text(spotCode);
        $summaryStart.text(startTimeStr);
        $summaryExpire.text(expireTimeStr);

        $summaryCard.show();
    }

    // ─── 9. SUBMIT CREATE RESERVATION ───────────────────────────────────────────
    $btnSubmit.on('click', function () {
        // 1. Basic client-side completeness check
        var areaId = $areaSelect.val();
        if (!areaId) {
            abp.message.warn('Please select a parking area.');
            return;
        }

        var areaOpt = $areaSelect.find('option:selected');
        var mode = areaOpt.data('mode');
        var spotId = null;

        if (mode === 0) {
            spotId = $spotSelect.val();
            if (!spotId) {
                abp.message.warn('Please select a parking spot.');
                return;
            }
        }

        var reservedAtVal = $reservedAtInput.val();
        if (!reservedAtVal) {
            abp.message.warn('Please select a reservation start time.');
            return;
        }

        var reservedAt = new Date(reservedAtVal);
        var now = new Date();
        if (reservedAt < now) {
            abp.message.warn('Reservation time cannot be in the past.');
            return;
        }

        var payload = {
            parkingAreaId: parseInt(areaId, 10),
            parkingSpotId: spotId ? parseInt(spotId, 10) : null,
            reservedAt: moment(reservedAtVal).toISOString()
        };

        // Prevent duplicate form submissions while processing
        abp.ui.setBusy($modal);
        $btnSubmit.prop('disabled', true);

        _reservationService.create(payload)
            .done(function () {
                $modal.modal('hide');
                abp.notify.success(l('ReservationCreatedSuccessfully'));
                loadReservations();
            })
            .fail(function () {
                // Handled by ABP AJAX interceptor (displays BusinessRuleException message)
            })
            .always(function () {
                abp.ui.clearBusy($modal);
                $btnSubmit.prop('disabled', false);
            });
    });

    // ─── 10. CANCEL RESERVATION ──────────────────────────────────────────────────
    function cancelReservation(reservationId) {
        if (!reservationId) return;

        abp.message.confirm(
            l('CancelReservationConfirmation'),
            l('CancelReservation'),
            function (isConfirmed) {
                if (isConfirmed) {
                    abp.ui.setBusy();
                    _reservationService.canceled({ id: reservationId })
                        .done(function () {
                            abp.notify.success(l('ReservationCancelledSuccessfully'));
                            loadReservations();
                        })
                        .always(function () {
                            abp.ui.clearBusy();
                        });
                }
            }
        );
    }

    // Cancel active card button
    $btnCancelActive.on('click', function () {
        var id = $(this).data('id');
        cancelReservation(id);
    });

    // Cancel button in history table
    $(document).on('click', '.btn-cancel-history', function () {
        var id = $(this).data('id');
        cancelReservation(id);
    });

    // ─── 11. MODAL TRIGGERS & FILTERS ───────────────────────────────────────────
    function openModal() {
        if (_activeReservation) {
            abp.message.info('You already have an active reservation. Please complete or cancel it before booking a new one.');
            return;
        }

        if (!_currentSubscription || _currentSubscription.status !== 1) {
            abp.message.warn(l('ActiveSubscriptionRequiredMessage'));
            return;
        }

        initModalFields();
        $modal.modal('show');
    }

    $btnOpenModal.on('click', openModal);
    $(document).on('click', '.btn-trigger-create', openModal);

    $historyStatusFilter.on('change', function () {
        renderHistoryTable();
    });

    $btnRefreshHistory.on('click', function () {
        loadReservations();
    });

    // ─── 12. INITIALIZATION ON PAGE LOAD ─────────────────────────────────────────
    checkSubscription();
    loadReservations();

})(jQuery);
