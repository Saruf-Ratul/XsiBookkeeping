(function () {
    'use strict';

    var cfg = window.__LEDGER__ || {};
    var apiUrl = cfg.apiUrl || '/Handlers/Api.ashx';
    var reasonTimers = {};

    function post(action, data) {
        return fetch(apiUrl + '?action=' + encodeURIComponent(action), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data || {})
        }).then(function (r) { return r.json(); });
    }

    function nextCheckClass(status) {
        if (status === 'done') return 'done';
        if (status === 'in-progress') return 'progress';
        return '';
    }

    function checkSymbol(status) {
        if (status === 'done') return '✓';
        if (status === 'in-progress') return '–';
        return '';
    }

    // Completion toggle
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-action="toggle-completion"]');
        if (!btn) return;
        e.preventDefault();
        var companyId = parseInt(btn.getAttribute('data-company-id'), 10);
        var accountId = parseInt(btn.getAttribute('data-account-id'), 10);
        var monthKey = btn.getAttribute('data-month-key');
        post('toggleCompletion', { companyId: companyId, accountId: accountId, monthKey: monthKey })
            .then(function (res) {
                if (!res.success) return;
                var status = res.data.status;
                btn.className = 'check-btn ' + nextCheckClass(status);
                btn.textContent = checkSymbol(status);
                var row = btn.closest('.account-row');
                if (row) {
                    row.style.background = status === 'done' ? '#fafffe' : status === 'in-progress' ? '#fffdf5' : '';
                    var nameEl = row.querySelector('.account-name');
                    if (nameEl) {
                        nameEl.classList.toggle('done', status === 'done');
                    }
                    var badge = row.querySelector('.account-badge');
                    if (badge) {
                        badge.className = 'account-badge ' + (status === 'done' ? 'badge-done' : status === 'in-progress' ? 'badge-progress' : 'hidden');
                        badge.textContent = status === 'done' ? 'Reconciled' : status === 'in-progress' ? 'In Progress' : '';
                    }
                }
            });
    });

    // Debounced overdue reason save
    document.addEventListener('input', function (e) {
        var input = e.target.closest('[data-action="save-reason"]');
        if (!input) return;
        var companyId = parseInt(input.getAttribute('data-company-id'), 10);
        var period = input.getAttribute('data-period');
        var statusEl = input.parentElement.querySelector('.reason-status');
        if (statusEl) {
            statusEl.className = 'reason-status saving';
            statusEl.textContent = 'Saving…';
        }
        clearTimeout(reasonTimers[companyId]);
        reasonTimers[companyId] = setTimeout(function () {
            post('saveReason', { companyId: companyId, period: period, reason: input.value })
                .then(function (res) {
                    if (statusEl) {
                        statusEl.className = 'reason-status' + (res.success ? ' saved' : '');
                        statusEl.textContent = res.success ? '✓ Saved' : '';
                        if (res.success) {
                            setTimeout(function () {
                                statusEl.className = 'reason-status';
                                statusEl.textContent = '·';
                            }, 2000);
                        }
                    }
                });
        }, 800);
    });

    // Comments
    document.addEventListener('keydown', function (e) {
        var ta = e.target.closest('[data-action="comment-input"]');
        if (ta && e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendComment(ta);
        }
    });

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-action="send-comment"]')) {
            var ta = e.target.closest('.comment-compose').querySelector('[data-action="comment-input"]');
            if (ta) sendComment(ta);
        }
        if (e.target.closest('[data-action="delete-comment"]')) {
            var delBtn = e.target.closest('[data-action="delete-comment"]');
            var commentId = parseInt(delBtn.getAttribute('data-comment-id'), 10);
            var companyId = parseInt(delBtn.getAttribute('data-company-id'), 10);
            post('deleteComment', { commentId: commentId }).then(function (res) {
                if (res.success) {
                    var row = delBtn.closest('.comment-row');
                    if (row) row.remove();
                    updateCommentCount(companyId);
                }
            });
        }
        if (e.target.closest('[data-action="expand-company"]')) {
            var expBtn = e.target.closest('[data-action="expand-company"]');
            var id = expBtn.getAttribute('data-company-id');
            var body = document.getElementById('company-body-' + id);
            if (body) {
                body.classList.toggle('open');
                expBtn.classList.toggle('open');
            }
        }
        if (e.target.closest('[data-action="show-add-account"]')) {
            var btn = e.target.closest('[data-action="show-add-account"]');
            var cid = btn.getAttribute('data-company-id');
            document.getElementById('add-account-' + cid).classList.remove('hidden');
            btn.classList.add('hidden');
        }
        if (e.target.closest('[data-action="cancel-add-account"]')) {
            var cancelBtn = e.target.closest('[data-action="cancel-add-account"]');
            var cid2 = cancelBtn.getAttribute('data-company-id');
            document.getElementById('add-account-' + cid2).classList.add('hidden');
            document.querySelector('[data-action="show-add-account"][data-company-id="' + cid2 + '"]').classList.remove('hidden');
        }
        if (e.target.closest('[data-action="add-account"]')) {
            var addBtn = e.target.closest('[data-action="add-account"]');
            var companyId = parseInt(addBtn.getAttribute('data-company-id'), 10);
            var input = document.querySelector('#add-account-' + companyId + ' input');
            var name = (input.value || '').trim();
            if (!name) return;
            post('upsertAccount', { companyId: companyId, name: name }).then(function (res) {
                if (res.success) location.reload();
            });
        }
        if (e.target.closest('[data-action="delete-account"]')) {
            if (!confirm('Delete this item?')) return;
            var delAcc = e.target.closest('[data-action="delete-account"]');
            post('deleteAccount', { accountId: parseInt(delAcc.getAttribute('data-account-id'), 10) })
                .then(function (res) { if (res.success) location.reload(); });
        }
        if (e.target.closest('[data-action="delete-company"]')) {
            if (!confirm('Delete this company and all its accounts?')) return;
            var delCo = e.target.closest('[data-action="delete-company"]');
            post('deleteCompany', { companyId: parseInt(delCo.getAttribute('data-company-id'), 10) })
                .then(function (res) { if (res.success) location.reload(); });
        }
        if (e.target.closest('[data-action="save-company-edit"]')) {
            var saveCo = e.target.closest('[data-action="save-company-edit"]');
            var editWrap = saveCo.closest('.edit-inline');
            post('upsertCompany', {
                companyId: parseInt(editWrap.getAttribute('data-company-id'), 10),
                name: editWrap.querySelector('[data-field="name"]').value.trim(),
                country: editWrap.getAttribute('data-country') || ''
            }).then(function (res) { if (res.success) location.reload(); });
        }
        if (e.target.closest('[data-action="save-account-edit"]')) {
            var saveAcc = e.target.closest('[data-action="save-account-edit"]');
            var editAccWrap = saveAcc.closest('.edit-inline');
            post('upsertAccount', {
                accountId: parseInt(editAccWrap.getAttribute('data-account-id'), 10),
                companyId: parseInt(editAccWrap.getAttribute('data-company-id'), 10),
                name: editAccWrap.querySelector('[data-field="name"]').value.trim()
            }).then(function (res) { if (res.success) location.reload(); });
        }
        if (e.target.closest('[data-action="edit-company"]')) {
            var editCo = e.target.closest('[data-action="edit-company"]');
            var cid3 = editCo.getAttribute('data-company-id');
            document.getElementById('company-view-' + cid3).classList.add('hidden');
            document.getElementById('company-edit-' + cid3).classList.remove('hidden');
        }
        if (e.target.closest('[data-action="cancel-company-edit"]')) {
            var cancelCo = e.target.closest('[data-action="cancel-company-edit"]');
            var cid4 = cancelCo.getAttribute('data-company-id');
            document.getElementById('company-view-' + cid4).classList.remove('hidden');
            document.getElementById('company-edit-' + cid4).classList.add('hidden');
        }
        if (e.target.closest('[data-action="edit-account"]')) {
            var editAcc = e.target.closest('[data-action="edit-account"]');
            var aid = editAcc.getAttribute('data-account-id');
            document.getElementById('account-view-' + aid).classList.add('hidden');
            document.getElementById('account-edit-' + aid).classList.remove('hidden');
        }
        if (e.target.closest('[data-action="cancel-account-edit"]')) {
            var cancelAcc = e.target.closest('[data-action="cancel-account-edit"]');
            var aid2 = cancelAcc.getAttribute('data-account-id');
            document.getElementById('account-view-' + aid2).classList.remove('hidden');
            document.getElementById('account-edit-' + aid2).classList.add('hidden');
        }
        if (e.target.closest('[data-action="toggle-country"]')) {
            var toggle = e.target.closest('[data-action="toggle-country"]');
            var wrap = toggle.closest('[data-country-wrap]');
            var val = toggle.getAttribute('data-country');
            var cur = wrap.getAttribute('data-country') || '';
            wrap.setAttribute('data-country', cur === val ? '' : val);
            wrap.querySelectorAll('[data-action="toggle-country"]').forEach(function (b) {
                b.classList.toggle('active', wrap.getAttribute('data-country') === b.getAttribute('data-country'));
            });
        }
        if (e.target.closest('[data-action="add-company"]')) {
            var addCoBtn = e.target.closest('[data-action="add-company"]');
            var box = addCoBtn.closest('.add-company-box');
            var nameInput = box.querySelector('[data-field="company-name"]');
            var countryWrap = box.querySelector('[data-country-wrap]');
            post('upsertCompany', {
                name: nameInput.value.trim(),
                country: countryWrap ? (countryWrap.getAttribute('data-country') || '') : ''
            }).then(function (res) { if (res.success) location.reload(); });
        }
        if (e.target.closest('[data-action="show-add-company"]')) {
            document.getElementById('add-company-form').classList.remove('hidden');
            document.getElementById('add-company-btn').classList.add('hidden');
        }
        if (e.target.closest('[data-action="cancel-add-company"]')) {
            document.getElementById('add-company-form').classList.add('hidden');
            document.getElementById('add-company-btn').classList.remove('hidden');
        }
        if (e.target.closest('[data-action="admin-add-user"]')) {
            var login = (document.getElementById('new-windows-login').value || '').trim();
            var displayName = (document.getElementById('new-display-name').value || '').trim();
            var password = (document.getElementById('new-password').value || '');
            var role = document.getElementById('new-role').value;
            if (!login || !password) { alert('Username and password are required.'); return; }
            post('upsertUser', { windowsLogin: login, displayName: displayName, role: role, isActive: true, password: password })
                .then(function (res) { if (res.success) location.reload(); else alert(res.error || 'Failed'); });
        }
        if (e.target.closest('[data-action="admin-save-user"]')) {
            var btn = e.target.closest('[data-action="admin-save-user"]');
            var userId = parseInt(btn.getAttribute('data-user-id'), 10);
            var row = btn.closest('tr');
            var displayName = row.querySelector('[data-field="displayName"]').value.trim();
            var role = row.querySelector('[data-field="role"]').value;
            var passwordField = row.querySelector('[data-field="password"]');
            var password = passwordField ? passwordField.value : '';
            var windowsLogin = btn.getAttribute('data-windows-login');
            var isActive = btn.getAttribute('data-active') === 'true';
            var payload = { appUserId: userId, windowsLogin: windowsLogin, displayName: displayName, role: role, isActive: isActive };
            if (password) payload.password = password;
            post('upsertUser', payload)
                .then(function (res) { if (res.success) location.reload(); else alert(res.error || 'Failed'); });
        }
        if (e.target.closest('[data-action="admin-deactivate-user"]')) {
            if (!confirm('Deactivate this user?')) return;
            var deactBtn = e.target.closest('[data-action="admin-deactivate-user"]');
            post('deactivateUser', { appUserId: parseInt(deactBtn.getAttribute('data-user-id'), 10) })
                .then(function (res) { if (res.success) location.reload(); else alert(res.error || 'Failed'); });
        }
        if (e.target.closest('[data-action="admin-activate-user"]')) {
            if (!confirm('Activate this user?')) return;
            var actBtn = e.target.closest('[data-action="admin-activate-user"]');
            post('activateUser', { appUserId: parseInt(actBtn.getAttribute('data-user-id'), 10) })
                .then(function (res) { if (res.success) location.reload(); else alert(res.error || 'Failed'); });
        }
        if (e.target.closest('[data-action="toggle-assign-co"]')) {
            var toggleBtn = e.target.closest('[data-action="toggle-assign-co"]');
            var co = toggleBtn.closest('.assign-co');
            var body = co ? co.querySelector('.assign-co-body') : null;
            if (!body) return;
            var open = body.classList.toggle('open');
            toggleBtn.classList.toggle('open', open);
            toggleBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    });

    var assignSaveTimers = {};

    function saveAssignmentRow(row) {
        if (!row) return;
        var accountId = parseInt(row.getAttribute('data-account-id'), 10);
        var wrap = row.querySelector('.assignment-users');
        var statusEl = row.querySelector('.assign-row-status');
        if (!wrap || !accountId) return;

        var ids = [];
        wrap.querySelectorAll('.assignment-check:checked').forEach(function (cb) {
            ids.push(parseInt(cb.getAttribute('data-user-id'), 10));
        });

        if (statusEl) {
            statusEl.textContent = 'Saving…';
            statusEl.className = 'assign-row-status is-saving';
        }

        clearTimeout(assignSaveTimers[accountId]);
        assignSaveTimers[accountId] = setTimeout(function () {
            post('setAssignments', { accountId: accountId, appUserIds: ids })
                .then(function (res) {
                    if (res.success) {
                        row.setAttribute('data-assigned', ids.length > 0 ? 'true' : 'false');
                        var dot = row.querySelector('.assign-row-dot');
                        if (dot) dot.classList.toggle('on', ids.length > 0);
                        if (statusEl) {
                            statusEl.textContent = 'Saved';
                            statusEl.className = 'assign-row-status is-saved';
                            setTimeout(function () {
                                if (statusEl.classList.contains('is-saved')) {
                                    statusEl.textContent = '';
                                    statusEl.className = 'assign-row-status';
                                }
                            }, 2000);
                        }
                        updateCompanyProgress(row.closest('.assign-co'));
                    } else {
                        if (statusEl) {
                            statusEl.textContent = 'Error';
                            statusEl.className = 'assign-row-status is-error';
                        }
                        showAssignToast(res.error || 'Failed to save', true);
                    }
                })
                .catch(function () {
                    if (statusEl) {
                        statusEl.textContent = 'Error';
                        statusEl.className = 'assign-row-status is-error';
                    }
                    showAssignToast('Network error — try again', true);
                });
        }, 350);
    }

    function updateCompanyProgress(co) {
        if (!co) return;
        var rows = co.querySelectorAll('.assign-row');
        var total = rows.length;
        var assigned = 0;
        rows.forEach(function (r) {
            if (r.getAttribute('data-assigned') === 'true') assigned++;
        });
        var stats = co.querySelector('.assign-co-stats');
        var fill = co.querySelector('.progress-bar-fill');
        if (stats) stats.textContent = assigned + '/' + total;
        if (fill && total > 0) {
            var pct = Math.round(assigned / total * 100);
            fill.style.width = pct + '%';
            fill.style.background = assigned === total ? '#15803d' : assigned > 0 ? '#f59e0b' : '#e8e4dc';
        }
    }

    function showAssignToast(message, isError) {
        var toast = document.getElementById('assign-toast');
        if (!toast) return;
        toast.textContent = message;
        toast.classList.toggle('is-error', !!isError);
        toast.classList.remove('hidden');
        clearTimeout(showAssignToast._timer);
        showAssignToast._timer = setTimeout(function () {
            toast.classList.add('hidden');
        }, 3200);
    }

    function initAssignmentsPage() {
        var page = document.querySelector('.assignments-page');
        if (!page) return;

        var searchInput = document.getElementById('assign-search');
        var filterBtns = page.querySelectorAll('[data-assign-filter]');
        var currentFilter = 'all';

        function syncPersonState(person) {
            var cb = person.querySelector('.assignment-check');
            if (!cb) return;
            person.classList.toggle('selected', cb.checked);
        }

        page.querySelectorAll('.assign-person').forEach(syncPersonState);

        page.querySelectorAll('.assignment-check').forEach(function (cb) {
            cb.addEventListener('change', function () {
                var person = cb.closest('.assign-person');
                var row = cb.closest('.assign-row');
                if (person) syncPersonState(person);
                if (row) saveAssignmentRow(row);
            });
        });

        function applyFilters() {
            var q = (searchInput && searchInput.value || '').trim().toLowerCase();
            var anyVisible = false;

            page.querySelectorAll('.assign-co').forEach(function (company) {
                var companyMatch = !q || (company.getAttribute('data-search') || '').indexOf(q) >= 0;
                var visibleTasks = 0;

                company.querySelectorAll('.assign-row').forEach(function (task) {
                    var assigned = task.getAttribute('data-assigned') === 'true';
                    var filterOk = currentFilter === 'all'
                        || (currentFilter === 'assigned' && assigned)
                        || (currentFilter === 'unassigned' && !assigned);
                    var title = (task.querySelector('.assign-row-name') || {}).textContent || '';
                    var taskMatch = !q || companyMatch || title.toLowerCase().indexOf(q) >= 0;
                    var show = filterOk && taskMatch;
                    task.classList.toggle('hidden', !show);
                    if (show) visibleTasks++;
                });

                company.classList.toggle('hidden', visibleTasks === 0);
                if (visibleTasks > 0) anyVisible = true;
            });

            var noResults = document.getElementById('assign-no-results');
            if (noResults) noResults.classList.toggle('hidden', anyVisible);
        }

        if (searchInput) {
            searchInput.addEventListener('input', applyFilters);
        }

        filterBtns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                currentFilter = btn.getAttribute('data-assign-filter') || 'all';
                filterBtns.forEach(function (b) { b.classList.toggle('active', b === btn); });
                applyFilters();
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAssignmentsPage);
    } else {
        initAssignmentsPage();
    }

    function sendComment(ta) {
        var text = (ta.value || '').trim();
        if (!text) return;
        var companyId = parseInt(ta.getAttribute('data-company-id'), 10);
        post('addComment', { companyId: companyId, content: text }).then(function (res) {
            if (!res.success || !res.data) return;
            var c = res.data;
            var scroll = ta.closest('.comments-panel').querySelector('.comment-scroll');
            var empty = scroll.querySelector('.comment-empty');
            if (empty) empty.remove();
            var row = document.createElement('div');
            row.className = 'comment-row';
            row.innerHTML =
                '<div class="comment-avatar" style="background:' + authorColor(c.author) + '">' + (c.author.charAt(0).toUpperCase()) + '</div>' +
                '<div class="comment-body">' +
                '<div class="comment-meta"><span class="comment-author" style="color:' + authorColor(c.author) + '">' + escapeHtml(c.author) + '</span>' +
                '<span class="comment-time">' + escapeHtml(c.formattedTime) + '</span></div>' +
                '<div>' + escapeHtml(c.content) + '</div></div>' +
                '<button type="button" class="delete-comment" data-action="delete-comment" data-comment-id="' + c.commentId + '" data-company-id="' + companyId + '">✕</button>';
            scroll.appendChild(row);
            ta.value = '';
            updateCommentCount(companyId);
        });
    }

    function updateCommentCount(companyId) {
        var panel = document.querySelector('.comments-panel[data-company-id="' + companyId + '"]');
        if (!panel) return;
        var count = panel.querySelectorAll('.comment-row').length;
        var el = panel.querySelector('.comments-count');
        if (el) el.textContent = count > 0 ? count : '';
    }

    function authorColor(name) {
        var colors = ['#c2410c', '#0369a1', '#15803d', '#7e22ce', '#be185d', '#b45309'];
        var h = 0;
        for (var i = 0; i < (name || '').length; i++) h = (h * 31 + name.charCodeAt(i)) % colors.length;
        return colors[h];
    }

    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }
})();
