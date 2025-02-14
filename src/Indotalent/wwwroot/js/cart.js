function updateHeader() {
    fetch('/Header?handler=Refresh')
        .then(response => {
            if (!response.ok) throw new Error("Erreur HTTP : " + response.status);
            return response.text();
        })
        .then(html => {
            document.getElementById("header-container").innerHTML = html;
            attachEventListeners();  // Rappelle cette fonction après mise à jour
        })
        .catch(error => console.error('Erreur lors de la mise à jour du panier:', error));
}

function addToCart(id, name, type, srcimage, qtyInput) {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenElement ? tokenElement.value : '';
    fetch('/Header?handler=AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            ProductId: id,
            Name: name,
            Type: type,
            SrcImage: srcimage,
            Quantity: qtyInput
        })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateHeader();
            }
        });
}

function deleteproduct(productId) {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenElement ? tokenElement.value : '';
    fetch('/Header?handler=DeleteProduct', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ productId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateHeader(); // Met à jour le panier après suppression
            }
        })
        .catch(error => console.error('Erreur lors de la suppression du produit:', error));
}

function attachEventListeners() {
    document.querySelectorAll('.delete').forEach(button => {
        button.onclick = function () {
            const productId = this.getAttribute('data-product-id');
            deleteproduct(productId);
        };
    });
}

// Exécuter l'attachement des événements au chargement initial
document.addEventListener("DOMContentLoaded", attachEventListeners);