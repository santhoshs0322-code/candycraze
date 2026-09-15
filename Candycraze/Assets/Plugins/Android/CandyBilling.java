package com.gamixtv.candycraze.billing;

import android.app.Activity;
import com.unity3d.player.UnityPlayer;
import com.android.billingclient.api.*;
import org.json.*;
import java.util.*;

/** Billing 8 bridge. Only the server verifies, grants and consumes purchases. */
public final class CandyBilling implements PurchasesUpdatedListener {
    private static CandyBilling instance;
    private final Activity activity;
    private final BillingClient client;
    private final Map<String, ProductDetails> products = new HashMap<>();
    private final Map<String, String> offers = new HashMap<>();
    private boolean connecting;
    private CandyBilling(Activity activity) {
        this.activity = activity;
        client = BillingClient.newBuilder(activity).setListener(this)
            .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
            .enableAutoServiceReconnection().build();
    }
    public static void initialize() {
        UnityPlayer.currentActivity.runOnUiThread(() -> {
            if (instance == null) instance = new CandyBilling(UnityPlayer.currentActivity);
            instance.connect();
        });
    }
    private static void send(String type, String product, String value, String account) {
        try {
            JSONObject data = new JSONObject();
            data.put("type", type); data.put("productId", product);
            data.put("value", value); data.put("accountId", account);
            UnityPlayer.UnitySendMessage("IAPManager", "OnBillingEvent", data.toString());
        } catch (JSONException ignored) { }
    }
    private void connect() {
        if (client.isReady()) { loadProducts(); queryOwned(); return; }
        if (connecting) return;
        connecting = true;
        client.startConnection(new BillingClientStateListener() {
            public void onBillingSetupFinished(BillingResult result) {
                connecting = false;
                if (result.getResponseCode() == BillingClient.BillingResponseCode.OK) {
                    loadProducts(); queryOwned();
                } else send("error", "", "Google Play billing unavailable. Please retry.", "");
            }
            public void onBillingServiceDisconnected() {
                connecting = false;
                send("disconnected", "", "Google Play disconnected. Please retry.", "");
            }
        });
    }
    private void loadProducts() {
        List<QueryProductDetailsParams.Product> items = new ArrayList<>();
        for (String id : new String[] { "coins_small", "coins_medium", "coins_large" })
            items.add(QueryProductDetailsParams.Product.newBuilder().setProductId(id)
                .setProductType(BillingClient.ProductType.INAPP).build());
        client.queryProductDetailsAsync(QueryProductDetailsParams.newBuilder().setProductList(items).build(),
            (result, response) -> activity.runOnUiThread(() -> {
                products.clear(); offers.clear();
                send("catalog", "", "", "");
                if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                    send("error", "", "Could not load Google Play prices. Please retry.", ""); return;
                }
                for (ProductDetails product : response.getProductDetailsList()) {
                    List<ProductDetails.OneTimePurchaseOfferDetails> options = product.getOneTimePurchaseOfferDetailsList();
                    if (options == null) continue;
                    for (ProductDetails.OneTimePurchaseOfferDetails option : options) {
                        if (!"standard-buy".equals(option.getPurchaseOptionId()) ||
                            (option.getOfferId() != null && !option.getOfferId().isEmpty())) continue;
                        products.put(product.getProductId(), product);
                        offers.put(product.getProductId(), option.getOfferToken());
                        send("price", product.getProductId(), option.getFormattedPrice(), "");
                        break;
                    }
                }
                send("ready", "", "", "");
            }));
    }
    public static void purchase(String productId, String accountId) {
        if (instance == null) { initialize(); send("error", "", "Store is connecting. Please retry.", ""); return; }
        instance.activity.runOnUiThread(() -> {
            ProductDetails product = instance.products.get(productId);
            if (!instance.client.isReady() || product == null) {
                instance.connect(); send("error", productId, "This pack is not available. Refresh the shop.", ""); return;
            }
            BillingFlowParams.ProductDetailsParams item = BillingFlowParams.ProductDetailsParams.newBuilder()
                .setProductDetails(product).setOfferToken(instance.offers.get(productId)).build();
            BillingResult result = instance.client.launchBillingFlow(instance.activity,
                BillingFlowParams.newBuilder().setProductDetailsParamsList(Collections.singletonList(item))
                    .setObfuscatedAccountId(accountId).build());
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                if (result.getResponseCode() == BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED) instance.queryOwned();
                send("error", productId, "Checkout could not open. Please restore purchases and retry.", "");
            }
        });
    }
    public static void restore() { initialize(); }
    private void queryOwned() {
        client.queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.INAPP).build(),
            (result, purchases) -> {
                if (result.getResponseCode() == BillingClient.BillingResponseCode.OK)
                    for (Purchase purchase : purchases) report(purchase);
            });
    }
    @Override public void onPurchasesUpdated(BillingResult result, List<Purchase> purchases) {
        if (result.getResponseCode() == BillingClient.BillingResponseCode.OK && purchases != null) {
            for (Purchase purchase : purchases) report(purchase);
        } else if (result.getResponseCode() == BillingClient.BillingResponseCode.USER_CANCELED) {
            send("cancelled", "", "Purchase cancelled. No crystals added.", "");
        } else send("error", "", "Payment could not complete. Please retry or restore purchases.", "");
    }
    private void report(Purchase purchase) {
        if (purchase.getPurchaseState() == Purchase.PurchaseState.PENDING) {
            send("pending", "", "Payment pending. Crystals arrive after Google confirms payment.", ""); return;
        }
        if (purchase.getPurchaseState() != Purchase.PurchaseState.PURCHASED) return;
        AccountIdentifiers identity = purchase.getAccountIdentifiers();
        String account = identity == null ? "" : identity.getObfuscatedAccountId();
        for (String product : purchase.getProducts())
            send("purchase", product, purchase.getPurchaseToken(), account == null ? "" : account);
    }
}
