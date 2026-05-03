/**
 * PaScan Client-Side Table Pagination
 * Auto-paginates any <table> with class "paginated-table".
 * Default: 10 rows per page. Override via data-page-size="N" on the table.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const tables = document.querySelectorAll('table.paginated-table');
        tables.forEach(initPagination);
    });

    function initPagination(table) {
        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        const allRows = Array.from(tbody.querySelectorAll('tr'));
        // Filter out empty-state rows (single row with colspan)
        const dataRows = allRows.filter(function (r) {
            const firstTd = r.querySelector('td');
            return firstTd && !firstTd.hasAttribute('colspan');
        });
        const emptyRows = allRows.filter(function (r) {
            const firstTd = r.querySelector('td');
            return firstTd && firstTd.hasAttribute('colspan');
        });

        const pageSize = parseInt(table.getAttribute('data-page-size')) || 10;

        // If no data rows at all, don't show pagination
        if (dataRows.length === 0) return;

        // Hide empty-state rows since we have data
        emptyRows.forEach(function (r) { r.style.display = 'none'; });

        let currentPage = 1;
        const totalPages = Math.ceil(dataRows.length / pageSize);

        // Create pagination wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'pg-wrapper';

        const controls = document.createElement('div');
        controls.className = 'pg-controls';
        wrapper.appendChild(controls);

        // Insert after the table's parent card (or after table itself)
        const tableWrap = table.closest('.adm-table-wrap') || table.closest('.adm-card') || table.parentNode;
        if (tableWrap && tableWrap !== table.parentNode) {
            tableWrap.appendChild(wrapper);
        } else {
            table.parentNode.insertBefore(wrapper, table.nextSibling);
        }

        function showPage(page) {
            currentPage = page;
            dataRows.forEach(function (row, i) {
                var start = (page - 1) * pageSize;
                var end = start + pageSize;
                row.style.display = (i >= start && i < end) ? '' : 'none';
            });
            renderControls();
        }

        function renderControls() {
            controls.innerHTML = '';

            // Prev button
            var prevBtn = createBtn('‹', currentPage <= 1);
            if (currentPage > 1) {
                prevBtn.addEventListener('click', function () { showPage(currentPage - 1); });
            }
            controls.appendChild(prevBtn);

            // Page numbers with ellipsis
            var pages = getPageNumbers(currentPage, totalPages);
            pages.forEach(function (p) {
                if (p === '...') {
                    var ell = document.createElement('span');
                    ell.className = 'pg-ellipsis';
                    ell.textContent = '…';
                    controls.appendChild(ell);
                } else {
                    var btn = createBtn(p, false, p === currentPage);
                    if (p !== currentPage) {
                        btn.addEventListener('click', function () { showPage(p); });
                    }
                    controls.appendChild(btn);
                }
            });

            // Next button
            var nextBtn = createBtn('›', currentPage >= totalPages);
            if (currentPage < totalPages) {
                nextBtn.addEventListener('click', function () { showPage(currentPage + 1); });
            }
            controls.appendChild(nextBtn);

            // Info text
            var info = document.createElement('span');
            info.className = 'pg-info';
            var start = (currentPage - 1) * pageSize + 1;
            var end = Math.min(currentPage * pageSize, dataRows.length);
            info.textContent = start + '–' + end + ' of ' + dataRows.length;
            controls.appendChild(info);
        }

        function createBtn(text, disabled, active) {
            var btn = document.createElement('button');
            btn.className = 'pg-btn' + (active ? ' active' : '');
            btn.textContent = text;
            btn.disabled = !!disabled;
            btn.type = 'button';
            return btn;
        }

        function getPageNumbers(current, total) {
            if (total <= 7) {
                var arr = [];
                for (var i = 1; i <= total; i++) arr.push(i);
                return arr;
            }
            var pages = [];
            pages.push(1);
            if (current > 3) pages.push('...');
            for (var j = Math.max(2, current - 1); j <= Math.min(total - 1, current + 1); j++) {
                pages.push(j);
            }
            if (current < total - 2) pages.push('...');
            pages.push(total);
            return pages;
        }

        showPage(1);
    }
})();
