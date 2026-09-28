(function ($) {
    'use strict';

    // ─── 1. SERVICES & LOCALIZATION ─────────────────────────────────────────────
    var _subscriptionService = abp.services.app.subscription,
        l = abp.localization.getSource('ParkingSystem');

    // ─── 2. STATE ────────────────────────────────────────────────────────────────
    var _selectedQuotationId = null;

    // ─── 3. DOM REFERENCES ──────────────────────────────────────────────────────
    var $loading          = $('#subscription-loading'),
        $emptyCard        = $('#subscription-empty'),
        $subCard          = $('#subscription-card'),
        $subCardInner     = $('#subscription-card-inner'),
        $historySection   = $('#history-section'),
        $historyTbody     = $('#history-tbody'),
        $historyEmpty     = $('#history-empty'),
        $buyBtn           = $('#btn-open-buy-modal'),
        $buyModal         = $('#BuySubscriptionModal'),
        $quotationLoading = $('#quotation-loading'),
        $quotationCards   = $('#quotation-cards'),
        $quotationEmpty   = $('#quotation-empty'),
        $startDateSection = $('#start-date-section'),
        $inputStartDate   = $('#input-start-date'),
        $planSummary      = $('#selected-plan-summary'),
        $confirmBtn       = $('#btn-confirm-purchase'),
        $cancelPlanBtn    = $('#btn-cancel-plan-selection');

    // ─── 4. STATUS HELPERS ───────────────────────────────────────────────────────
    var statusConfig = {
        0: { label: l('Pending'),  cls: 'badge-warning  bg-warning'  },
        1: { label: l('InUse'),    cls: 'badge-success  bg-success'  },
        2: { label: l('Expired'),  cls: 'badge-secondary bg-secondary' },
        3: { label: l('Canceled'), cls: 'badge-danger   bg-danger'   }
    };

    function statusBadge(status) {
        var cfg = statusConfig[status] || { label: status, cls: 'badge-light' };
        return '<span class="badge ' + cfg.cls + ' px-3 py-2" style="font-size:.85rem;">' + cfg.label + '</span>';
    }

    // Card border colour based on status
    var cardColorMap = { 0: 'card-warning', 1: 'card-success', 2: 'card-secondary', 3: 'card-danger' };

    function fmtDate(val) {
        return val ? moment(val).format('YYYY-MM-DD') : '—';
    }

    function fmtPrice(val) {
        return val != null
            ? val.toLocaleString(undefined, { minimumFractionDigits: 0 }) + ' ₫'
            : '—';
    }

    // ─── 5. VEHICLE TYPE ICON MAP ────────────────────────────────────────────────
    var vehicleIcons = {
        'Bicycle':            'fa-bicycle',
        'ElectricBicycle':    'fa-bicycle',
        'Motorcycle':         'fa-motorcycle',
        'ElectricMotorcycle': 'fa-motorcycle',
        'Car':                'fa-car'
    };

    function vehicleIcon(typeName) {
        return vehicleIcons[typeName] || 'fa-car';
    }

    // ─── 6. LOAD CURRENT SUBSCRIPTION ───────────────────────────────────────────
    function loadCurrentSubscription() {
        $loading.show();
        $emptyCard.hide();
        $subCard.hide();

        _subscriptionService.getMyCurrentSubscription()
            .done(function (sub) {
                $loading.hide();

                if (!sub) {
                    // No active / pending subscription → show empty + buy button
                    $emptyCard.show();
                    loadHistory();
                    return;
                }

                // ─ populate info-boxes
                $('#sub-vehicle-type').text(sub.vehicleTypeName || sub.vehicleType);
                $('#sub-duration').text(sub.duration + ' ' + sub.durationUnitName);
                $('#sub-price').text(fmtPrice(sub.price));
                $('#sub-start-time').text(fmtDate(sub.startTime));
                $('#sub-end-time').text(fmtDate(sub.endTime));

                // status badge in header
                $('#subscription-status-badge').html(statusBadge(sub.status));

                // colour the card border by status
                $subCardInner.removeClass('card-warning card-success card-secondary card-danger card-primary');
                $subCardInner.addClass('card-outline ' + (cardColorMap[sub.status] || 'card-primary'));

                $subCard.show();

                // Do NOT show "Buy" when status is pending(0) or inUse(1)
                if (sub.status === 0 || sub.status === 1) {
                    $buyBtn.hide();
                } else {
                    $buyBtn.show();
                }

                loadHistory();
            })
            .fail(function () {
                $loading.hide();
                $emptyCard.show();
                loadHistory();
            });
    }

    // ─── 7. LOAD HISTORY TABLE ───────────────────────────────────────────────────
    function loadHistory() {
        _subscriptionService.getAll({ maxResultCount: 50, skipCount: 0 })
            .done(function (result) {
                $historyTbody.empty();

                var items = result.items || [];
                if (items.length === 0) {
                    $historyEmpty.show();
                    $historySection.show();
                    return;
                }

                $historyEmpty.hide();

                $.each(items, function (_, row) {
                    var tr = $('<tr>');
                    tr.append($('<td>').text(row.vehicleTypeName || row.vehicleType));
                    tr.append($('<td>').text(row.duration + ' ' + row.durationUnitName));
                    tr.append($('<td>').text(fmtPrice(row.price)));
                    tr.append($('<td>').text(fmtDate(row.startTime)));
                    tr.append($('<td>').text(fmtDate(row.endTime)));
                    tr.append($('<td>').html(statusBadge(row.status)));
                    $historyTbody.append(tr);
                });

                $historySection.show();
            })
            .fail(function () {
                $historySection.show();
            });
    }

    // ─── 8. LOAD AVAILABLE QUOTATIONS (plan cards) ───────────────────────────────
    function loadQuotationCards() {
        $quotationLoading.show();
        $quotationCards.hide().empty();
        $quotationEmpty.hide();
        $startDateSection.hide();
        _selectedQuotationId = null;

        _subscriptionService.getAvailableQuotations()
            .done(function (quotations) {
                $quotationLoading.hide();

                if (!quotations || quotations.length === 0) {
                    $quotationEmpty.show();
                    return;
                }

                $.each(quotations, function (_, q) {
                    var icon = vehicleIcon(q.vehicleTypeName || '');
                    var card = $(
                        '<div class="col-sm-6 col-md-4 col-lg-3">' +
                        '  <div class="card h-100 plan-card border-0 shadow-sm" ' +
                        '       data-id="' + q.id + '" ' +
                        '       data-vehicle="' + (q.vehicleTypeName || q.vehicleType) + '" ' +
                        '       data-duration="' + q.duration + '" ' +
                        '       data-unit="' + (q.durationUnitType || q.durationUnit) + '" ' +
                        '       data-price="' + q.price + '">' +
                        '    <div class="card-body text-center py-4">' +
                        '      <i class="fas ' + icon + ' fa-3x text-primary mb-3 d-block"></i>' +
                        '      <h5 class="card-title mb-1">' + (q.vehicleTypeName || q.vehicleType) + '</h5>' +
                        '      <p class="text-muted mb-2">' + q.duration + ' ' + (q.durationUnitType || q.durationUnit) + '</p>' +
                        '      <h4 class="text-success font-weight-bold fw-bold">' + fmtPrice(q.price) + '</h4>' +
                        '    </div>' +
                        '    <div class="card-footer bg-transparent border-0 text-center pb-3">' +
                        '      <button type="button" class="btn btn-outline-primary btn-select-plan w-100" data-id="' + q.id + '">' +
                        '        <i class="fas fa-check-circle mr-1 me-1"></i>' + l('SelectPlan') +
                        '      </button>' +
                        '    </div>' +
                        '  </div>' +
                        '</div>'
                    );
                    $quotationCards.append(card);
                });

                $quotationCards.show();
            })
            .fail(function () {
                $quotationLoading.hide();
                $quotationEmpty.show();
            });
    }

    // ─── 9. PLAN SELECTION ───────────────────────────────────────────────────────
    $(document).on('click', '.btn-select-plan', function () {
        var id       = $(this).data('id');
        var $card    = $(this).closest('.plan-card');
        var vehicle  = $card.data('vehicle');
        var duration = $card.data('duration');
        var unit     = $card.data('unit');
        var price    = $card.data('price');

        _selectedQuotationId = id;

        // Highlight selected card
        $('.plan-card').removeClass('border border-primary shadow');
        $card.addClass('border border-primary shadow');

        // Set today as default start date min
        var today = moment().format('YYYY-MM-DD');
        $inputStartDate.val(today).attr('min', today);

        // Summary
        $planSummary.html(
            '<strong>' + vehicle + '</strong> &middot; ' +
            duration + ' ' + unit + ' &middot; ' +
            '<strong>' + fmtPrice(price) + '</strong>'
        ).show();

        $startDateSection.show();
        $startDateSection[0].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    });

    $cancelPlanBtn.on('click', function () {
        $startDateSection.hide();
        _selectedQuotationId = null;
        $('.plan-card').removeClass('border border-primary shadow');
    });

    // ─── 10. CONFIRM PURCHASE ────────────────────────────────────────────────────
    $confirmBtn.on('click', function () {
        if (!_selectedQuotationId) {
            abp.message.warn('Please select a plan first.');
            return;
        }

        var startDateVal = $inputStartDate.val();
        if (!startDateVal) {
            abp.message.warn('Please select a start date.');
            return;
        }

        var startDate = new Date(startDateVal);
        var today     = new Date();
        today.setHours(0, 0, 0, 0);

        if (startDate < today) {
            abp.message.warn('Start date must be today or in the future.');
            return;
        }

        var input = {
            quotationId: _selectedQuotationId,
            startTime: startDateVal
        };

        abp.ui.setBusy($buyModal);
        _subscriptionService.create(input)
            .done(function () {
                $buyModal.modal('hide');
                abp.notify.success(l('PurchaseSuccess'));
                loadCurrentSubscription();
            })
            .fail(function () {
                // abp interceptor already shows the error message
            })
            .always(function () {
                abp.ui.clearBusy($buyModal);
            });
    });

    // ─── 11. OPEN BUY MODAL ──────────────────────────────────────────────────────
    $buyBtn.on('click', function () {
        $buyModal.modal('show');
    });

    $buyModal.on('show.bs.modal', function () {
        loadQuotationCards();
    }).on('hidden.bs.modal', function () {
        $startDateSection.hide();
        _selectedQuotationId = null;
        $('.plan-card').removeClass('border border-primary shadow');
    });

    // ─── 12. BOOTSTRAP PAGE LOAD ─────────────────────────────────────────────────
    loadCurrentSubscription();

})(jQuery);
