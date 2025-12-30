// Centralized site JS: mini-cart (local fallback), categories loader, pending count, navbar auto-hide

// Utility: Format relative time (3 hours ago, yesterday, etc) - REUSABLE
window.formatRelativeTime = function(dateString) {
    try {
        var reviewDate = new Date(dateString);
        var now = new Date();
        var diffMs = now - reviewDate;
        var diffMins = Math.floor(diffMs / 60000);
        var diffHours = Math.floor(diffMs / 3600000);
        var diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'tocmai acum';
        if (diffMins < 60) return diffMins + ' min în urmă';
        if (diffHours < 1) return 'o oră în urmă';
        if (diffHours < 24) return diffHours + ' ore în urmă';
        if (diffDays === 1) return 'ieri';
        if (diffDays < 7) return diffDays + ' zile în urmă';
        if (diffDays < 30) return Math.floor(diffDays / 7) + ' săptămâni în urmă';

        // Fallback to full date with time
        var options = { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' };
        return reviewDate.toLocaleDateString('ro-RO', options);
    } catch (e) {
        console.error('[TimeFormat] Error:', e);
        return 'data necunoscută';
    }
};

// Initialize relative timestamps on any page
window.updateRelativeTimestamps = function(containerSelector) {
    var container = containerSelector ? document.querySelector(containerSelector) : document;
    var elements = container.querySelectorAll('[data-review-date]');
    elements.forEach(function(elem) {
        var dateStr = elem.getAttribute('data-review-date');
        if (dateStr) {
            elem.textContent = window.formatRelativeTime(dateStr);
            elem.title = elem.getAttribute('data-review-date'); // Full ISO date on hover
        }
    });
};

window.getLocalCart = function () {
    try { return JSON.parse(localStorage.getItem('localCart') || '[]'); } catch (e) { return []; }
};

// Toast helper (Bootstrap 5)
window.showToast = function(message, type) {
    // type: 'success','info','warning','danger'
    var toastRoot = document.getElementById('siteToasts');
    if (!toastRoot) {
        toastRoot = document.createElement('div');
        toastRoot.id = 'siteToasts';
        toastRoot.style.position = 'fixed';
        toastRoot.style.right = '20px';
        toastRoot.style.bottom = '20px';
        toastRoot.style.zIndex = '2000';
        document.body.appendChild(toastRoot);
    }
    var toast = document.createElement('div');
    toast.className = 'toast align-items-center text-bg-' + (type||'primary') + ' border-0 show';
    toast.setAttribute('role','alert');
    toast.setAttribute('aria-live','assertive');
    toast.setAttribute('aria-atomic','true');
    toast.style.minWidth = '220px';
    toast.style.marginTop = '8px';
    toast.innerHTML = '<div class="d-flex"><div class="toast-body">'+message+'</div><button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close"></button></div>';
    toastRoot.appendChild(toast);
    // auto remove after 3s
    setTimeout(function(){ toast.remove(); }, 3000);
    toast.querySelector('.btn-close')?.addEventListener('click', function(){ toast.remove(); });
};

// Add to cart detailed (used on product show page)
window.addToCartDetailed = function(productId, qty, btn) {
    qty = parseInt(qty) || 1;
    if (qty < 1) qty = 1;
    
    console.log('[CartDebug] Adding product:', productId, 'Qty:', qty, 'Authenticated:', window.isAuthenticated);
    
    if (window.isAuthenticated) {
        try {
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : '';

            const bodyStr = `productId=${productId}&quantity=${qty}` + (token ? `&__RequestVerificationToken=${encodeURIComponent(token)}` : '');

            fetch('/Orders/AddToCart', { 
                method: 'POST', 
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, 
                body: bodyStr
            })
        .then(r => r.json())
        .then(d => {
            console.log('[CartDebug] Server response:', d);
            if (d.success) { 
                loadMiniCart(); 
                showToast('Produs adăugat în coș', 'success'); 
            } else {
                showToast(d.message || 'Eroare la adăugare', 'danger');
            }
        })
        .catch(e=> { 
            console.error('[CartDebug] Error:', e); 
            showToast('Eroare rețea', 'danger'); 
        });
    } else {
        console.log('[CartDebug] User not authenticated, using localStorage');
        addToLocalCart(productId, qty, btn || document.querySelector('[onclick*="addToCartDetailed"]'));
        showToast('Produs adăugat în coș (local)', 'success');
    }
};

// Wishlist toggle detailed
window.toggleWishlistDetailed = function(productId, btn) {
    console.log('[WishlistDebug] Toggling product:', productId);
    
    try {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';
        const bodyStr = `productId=${productId}` + (token ? `&__RequestVerificationToken=${encodeURIComponent(token)}` : '');

        fetch('/Wishlist/Toggle', { 
            method: 'POST', 
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, 
            body: bodyStr 
        })
    .then(r => r.json())
    .then(d => {
        console.log('[WishlistDebug] Server response:', d);
        if (d.success) {
            const icon = btn.querySelector('i');
            if (!icon) {
                console.error('[WishlistDebug] Icon element not found in button');
                return;
            }
            
            if (d.action === 'added') { 
                icon.className = 'bi bi-heart-fill text-danger'; 
                showToast('Adăugat la favorite', 'success'); 
            } else { 
                icon.className = 'bi bi-heart'; 
                showToast('Eliminat din favorite', 'info'); 
            }
            
            // update wishlist badge if server returns count
            if (d.wishlistCount !== undefined) {
                var wb = document.getElementById('wishlist-badge-count');
                if (wb) {
                    wb.textContent = d.wishlistCount;
                    wb.style.display = d.wishlistCount > 0 ? 'inline-block' : 'none';
                }
            } else {
                // fallback: toggle increment/decrement
                var wb = document.getElementById('wishlist-badge-count');
                if (wb) {
                    var val = parseInt(wb.textContent || '0');
                    if (d.action === 'added') val += 1; else val = Math.max(0, val-1);
                    wb.textContent = val; 
                    wb.style.display = val>0 ? 'inline-block' : 'none';
                }
            }
            loadMiniCart();
        } else {
            showToast(d.message || 'Eroare', 'danger');
        }
    })
    .catch(e => { 
        console.error('[WishlistDebug] Error:', e); 
        showToast('Eroare rețea', 'danger'); 
    });
};

window.saveLocalCart = function (cart) {
    localStorage.setItem('localCart', JSON.stringify(cart));
};

window.addToLocalCart = function (productId, qty, btn) {
    var cart = window.getLocalCart();
    var id = productId.toString();
    var item = cart.find(i => i.productId == id);
    qty = parseInt(qty || 1);
    if (item) {
        item.quantity = (item.quantity || 0) + qty;
    } else {
        var title = '';
        var image = '';
        var price = 0;
        try {
            if (btn) {
                var card = btn.closest('.card') || btn.closest('.product-card') || document.body;
                var titleEl = card.querySelector('.card-title a') || card.querySelector('.card-title') || card.querySelector('h5');
                if (titleEl) title = titleEl.innerText.trim();
                var imgEl = card.querySelector('img');
                if (imgEl) image = imgEl.src;
                var priceEl = card.querySelector('.text-danger') || card.querySelector('.fs-4') || card.querySelector('.card-price');
                if (priceEl) {
                    var txt = priceEl.innerText.replace(/[^0-9.,]/g, '').replace(',', '.');
                    price = parseFloat(txt) || 0;
                }
            }
        } catch (e) { }
        item = { productId: id, quantity: qty, title: title, image: image, unitPrice: price };
        cart.push(item);
    }
    window.saveLocalCart(cart);
    window.renderLocalMiniCart(document.getElementById('miniCartContainer'));
    window.updateLocalBadge();
};

window.updateLocalBadge = function () {
    var cart = window.getLocalCart();
    var total = cart.reduce((s, i) => s + (i.quantity || 0), 0);
    var badge = document.getElementById('cart-badge-count');
    if (badge) {
        if (total > 0) { badge.textContent = total; badge.style.display = 'inline-block'; }
        else { badge.style.display = 'none'; }
    }
};

window.renderLocalMiniCart = function (container) {
    if (!container) return;
    var cart = window.getLocalCart();
    if (!cart || cart.length === 0) {
        container.innerHTML = '<div class="text-center py-3 text-muted"><i class="bi bi-cart-x"></i> Coș gol</div>';
        return;
    }
    var html = '';
    var itemsToShow = cart.slice(0,5);
    var moreCount = cart.length - itemsToShow.length;
    itemsToShow.forEach(function (item) {
        html += '<div class="d-flex align-items-center mb-2">';
        html += '<div style="width:56px; height:56px; background:#f8f9fa; display:flex; align-items:center; justify-content:center; overflow:hidden; border-radius:8px;">';
        if (item.image) html += '<img src="'+item.image+'" style="max-width:100%; max-height:100%; object-fit:contain;" />';
        else html += '<i class="bi bi-image text-muted fs-4"></i>';
        html += '</div>';
        html += '<div class="ms-2 flex-grow-1">';
        html += '<div class="fw-semibold text-dark" style="font-size:0.9rem;">'+(item.title||('Produs #'+item.productId))+'</div>';
        html += '<div class="text-muted small">'+(item.quantity||0)+' x '+(item.unitPrice||0)+' RON</div>';
        html += '</div>';
        html += '<div class="ms-2 text-end small fw-bold">'+((item.quantity||0)*(item.unitPrice||0))+' RON</div>';
        html += '</div>';
    });
    if (moreCount > 0) {
        html += '<div class="text-center small text-muted">+ '+moreCount+' alte produse în coș &middot; <a href="/Orders/Index">Vezi toate</a></div>';
    }
    html += '<div class="dropdown-divider my-2"></div>';
    var total = cart.reduce((s, i) => s + ((i.quantity||0)*(i.unitPrice||0)), 0);
    html += '<div class="d-flex justify-content-between align-items-center">';
    html += '<div><div class="small text-muted">Total:</div><div class="fw-bold">'+total+' RON</div></div>';
    html += '<div class="d-flex gap-2"><a class="btn btn-sm btn-outline-secondary mini-cart-action" href="/Orders/Index">Vezi coș</a><a class="btn btn-sm btn-primary mini-cart-action ms-2" href="/Orders/Checkout">Finalizează</a></div>';
    html += '</div>';
    container.innerHTML = html;
};

// Load mini cart from server, fallback to local cart if server asks for auth or fails
window.loadMiniCart = function () {
    const container = document.getElementById('miniCartContainer');
    if (!container) return;
    container.innerHTML = '<div class="text-center py-3 text-muted"><i class="bi bi-hourglass-split"></i> Se încarcă...</div>';
    fetch('/OrdersAjax/MiniCart')
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            if (html && html.indexOf('Autentificare necesară') !== -1) {
                window.renderLocalMiniCart(container);
                window.updateLocalBadge();
                return;
            }
            container.innerHTML = html;
            fetch('/OrdersAjax/MiniCartCount')
                .then(r => r.json())
                .then(data => {
                    const badge = document.getElementById('cart-badge-count');
                    if (badge) {
                        if (data.count > 0) { badge.textContent = data.count; badge.style.display = 'inline-block'; }
                        else { badge.style.display = 'none'; }
                    }
                }).catch(err => console.debug('MiniCartCount fetch failed', err));
        })
        .catch(err => {
            window.renderLocalMiniCart(container);
            window.updateLocalBadge();
            console.error(err);
        });
};

// Categories dropdown loader
window.loadCategoriesDropdown = function() {
    fetch('/Categories/GetAll')
        .then(response => response.json())
        .then(data => {
            const menu = document.getElementById('categoriesDropdownMenu');
            if (data && data.length > 0) {
                let html = '<li><a class="dropdown-item" href="/Products/Index"><i class="bi bi-list-ul"></i> Toate produsele</a></li>';
                html += '<li><hr class="dropdown-divider"></li>';
                data.forEach(function(category) {
                    if (category.productCount > 0) {
                        html += '<li><a class="dropdown-item" href="/Products/Index?category=' + category.id + '"><i class="bi bi-tag"></i> ' + category.name + ' <span class="badge bg-secondary ms-2">' + category.productCount + '</span></a></li>';
                    }
                });
                menu.innerHTML = html;
            } else {
                menu.innerHTML = '<li><a class="dropdown-item" href="/Products/Index"><i class="bi bi-list-ul"></i> Toate produsele</a></li>';
            }
        })
        .catch(function(error) { console.error('Eroare la încărcarea categoriilor:', error); });
};

// Pending count (for admin)
window.loadPendingCount = function() {
    fetch('/Products/GetPendingCount')
        .then(function(response) { if (!response.ok) throw new Error('Network response was not ok'); return response.json(); })
        .then(function(data) {
            const badge = document.getElementById('pendingBadge');
            const countNav = document.getElementById('pendingCountNav');
            if (data.count > 0) {
                if (badge) { badge.textContent = data.count; badge.style.display = 'inline-block'; }
                if (countNav) countNav.textContent = data.count;
            } else {
                if (badge) badge.style.display = 'none';
                if (countNav) countNav.textContent = '0';
            }
        })
        .catch(function(error) { console.log('Info: Pending count check skipped or failed.'); });
};

// Auto-hide navbar on scroll - IMPROVED
(function() {
    var lastScroll = 0;
    var navbar = null;
    var ticking = false;

    function updateNavbar() {
        var current = window.pageYOffset || document.documentElement.scrollTop;
        if (navbar) {
            // Hide navbar when scrolling DOWN and current > 100px
            // Show navbar when scrolling UP
            if (current > lastScroll && current > 100) {
                navbar.classList.add('navbar-hidden');
            } else {
                navbar.classList.remove('navbar-hidden');
            }
        }
        lastScroll = current <= 0 ? 0 : current;
        ticking = false;
    }

    document.addEventListener('DOMContentLoaded', function() {
        navbar = document.querySelector('.navbar');
        if (!navbar) return;

        window.addEventListener('scroll', function() {
            if (!ticking) {
                window.requestAnimationFrame(updateNavbar);
                ticking = true;
            }
        }, { passive: true });
    });
})();

// Initialize on DOMContentLoaded
document.addEventListener('DOMContentLoaded', function() {
    try { if (typeof window.loadCategoriesDropdown === 'function') loadCategoriesDropdown(); } catch (e) {}
    try { if (window.isAdmin) { loadPendingCount(); setInterval(loadPendingCount, 30000); } } catch (e) {}
    try { updateLocalBadge(); } catch (e) {}
    try { loadMiniCart(); } catch (e) {}
    try { window.updateRelativeTimestamps(); } catch (e) { console.debug('updateRelativeTimestamps failed', e); }
    try {
        // If user just logged in and we have a local cart, merge it server-side
        if (window.isAuthenticated) {
            var local = window.getLocalCart();
            if (local && local.length > 0) {
                    try {
                        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                        const token = tokenInput ? tokenInput.value : '';

                        const headers = { 'Content-Type': 'application/json' };
                        if (token) headers['RequestVerificationToken'] = token;

                        fetch('/OrdersAjax/MergeLocalCart', {
                            method: 'POST',
                            headers: headers,
                            body: JSON.stringify(local.map(i => ({ productId: parseInt(i.productId), quantity: parseInt(i.quantity) })))
                        }).then(r => r.json()).then(data => {
                    if (data && data.success) {
                        // clear local only if merged
                        localStorage.removeItem('localCart');
                        updateLocalBadge();
                        loadMiniCart();
                        if (data.merged && data.merged > 0) showToast('Coșul local a fost sincronizat', 'success');
                    }
                }).catch(err => console.debug('MergeLocalCart failed', err));
                    } catch (e) { console.debug('MergeLocalCart token attach failed', e); }
            }
        }
    } catch (e) { console.debug(e); }
});
