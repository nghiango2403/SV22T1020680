// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function changeMainImage(src) {
    document.getElementById('mainProductImage').src = src;
}

function updateQty(val) {
    let input = document.getElementById('quantity');
    let current = parseInt(input.value);
    if (current + val >= 1) {
        input.value = current + val;
    }
}

function addToCart(productId) {
    const data = new URLSearchParams();
    data.append('productId', productId);
    data.append('quantity', 1);

    fetch('/Order/AddCartItem', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: data
    })
        .then(response => response.json())
        .then(result => {
            if (result.code === 1) {
                const badge = document.getElementById('cart-count');

                if (badge) {
                    // Kiểm tra xem ID này đã có trong giỏ hàng chưa
                    // Chú ý: Cần ép kiểu productId về number để so sánh chính xác
                    const id = parseInt(productId);

                    if (!window.currentCartProductIds.includes(id)) {
                        // Nếu chưa có, tăng số lượng loại hàng lên 1
                        let currentCount = parseInt(badge.innerText) || 0;
                        badge.innerText = currentCount + 1;

                        // Thêm ID này vào mảng để nếu bấm thêm lần nữa sẽ không tăng số Badge
                        window.currentCartProductIds.push(id);

                        // Hiện badge nếu trước đó bằng 0
                        badge.classList.remove('d-none');
                    }
                }
                alert("Đã thêm vào giỏ hàng thành công!");
            } else {
                alert("Lỗi: " + result.message);
            }
        })
        .catch(error => {
            console.error('Lỗi:', error);
        });
}
// Hiển thị ảnh được chọn từ input file lên thẻ img
// (Thẻ input có thuộc tính data-img-preview trỏ đến id của thẻ img dung để hiển thị ảnh)
function previewImage(input) {
    if (!input.files || !input.files[0]) return;

    const previewId = input.dataset.imgPreview; // lấy data-img-preview
    if (!previewId) return;

    const img = document.getElementById(previewId);
    if (!img) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url = form.action;
    const targetId = form.dataset.target || "searchResult";
    const targetEl = document.getElementById(targetId);

    if (!targetEl) return;

    // Hiển thị trạng thái đang tải
    targetEl.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>`;

    // Lấy dữ liệu từ Form và thêm số trang
    const formData = new FormData(form);
    const params = new URLSearchParams();

    formData.forEach((value, key) => {
        // Chỉ thêm các giá trị có dữ liệu để URL gọn sạch
        if (value !== "" && value !== null) {
            params.append(key, value);
        }
    });
    params.set("Page", page); // Lưu ý: Chữ "P" viết hoa nếu Model của bạn dùng "Page"

    fetch(url + "?" + params.toString(), {
        method: "GET",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    })
        .then(res => res.text())
        .then(html => {
            targetEl.innerHTML = html; // Nhét bảng vào div searchResult
        })
        .catch(err => {
            targetEl.innerHTML = `<div class="alert alert-danger">Không thể tải dữ liệu: ${err}</div>`;
        });
}

// Mở modal và load nội dung từ link vào modal
(function () {
    //dialogModal là id của modal dùng chung đuơc định nghĩa trong _Layout.cshtml
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;

    const modalContent = modalEl.querySelector(".modal-content");

    // Clear nội dung khi modal đóng
    modalEl.addEventListener('hidden.bs.modal', function () {
        modalContent.innerHTML = '';
    });

    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();

        const url = link.getAttribute("href");

        // Hiển thị loading
        modalContent.innerHTML = `
            <div class="modal-body text-center py-5">
                <span>Đang tải dữ liệu...</span>
            </div>`;

        // Khởi tạo modal (chỉ tạo 1 lần)
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl, {
                backdrop: 'static',
                keyboard: false
            });
        }

        modal.show();

        // Load nội dung
        fetch(url)
            .then(res => res.text())
            .then(html => {
                modalContent.innerHTML = html;
            })
            .catch(() => {
                modalContent.innerHTML = `
                    <div class="modal-body text-danger">
                        Không tải được dữ liệu
                    </div>`;
            });
    };
})();

