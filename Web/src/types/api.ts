// Shared API types that mirror the C# DTOs.

export interface Product {
  id: string;
  sku: string;
  barcode?: string | null;
  nameAr: string;
  nameEn?: string | null;
  categoryId: string;
  categoryName?: string | null;
  unitId: string;
  unitName?: string | null;
  purchasePrice: number;
  salePrice: number;
  minSalePrice: number;
  vatRate: number;
  minStockLevel: number;
  maxStockLevel: number;
  trackStock: boolean;
  isActive: boolean;
  currentStock: number;
}

export interface Warehouse {
  id: string;
  nameAr: string;
  nameEn?: string | null;
  code: string;
  isMain: boolean;
  isActive: boolean;
}

export interface Customer {
  id: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  taxRegistrationNumber?: string | null;
  nationalId?: string | null;
  isCompany: boolean;
  balance: number;
  creditLimit: number;
  isActive: boolean;
}

export interface SaleItem {
  id: string;
  productId: string;
  productNameSnapshot: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  discountPercent: number;
  vatRate: number;
  vatAmount: number;
  lineSubTotal: number;
  lineTotal: number;
}

export interface SalePayment {
  id: string;
  method: number;
  amount: number;
  reference?: string | null;
  paidAt: string;
}

export interface Sale {
  id: string;
  invoiceNumber: string;
  customerId?: string | null;
  customerName?: string | null;
  warehouseId: string;
  warehouseName?: string | null;
  cashSessionId: string;
  cashierUserId: string;
  saleDate: string;
  subTotal: number;
  discountAmount: number;
  discountPercent: number;
  vatAmount: number;
  total: number;
  paidAmount: number;
  changeAmount: number;
  status: number;
  eInvoiceUuid?: string | null;
  eInvoiceStatus?: number | null;
  notes?: string | null;
  items: SaleItem[];
  payments: SalePayment[];
}

export interface CashRegister {
  id: string;
  name: string;
  code: string;
  warehouseId: string;
  warehouseName?: string | null;
  isActive: boolean;
  hasOpenSession: boolean;
}

export interface CashSession {
  id: string;
  cashRegisterId: string;
  cashRegisterName?: string | null;
  warehouseId: string;
  warehouseName?: string | null;
  cashierUserId: string;
  openedAt: string;
  closedAt?: string | null;
  openingBalance: number;
  closingBalance: number;
  expectedBalance: number;
  difference: number;
  totalCashSales: number;
  totalCardSales: number;
  totalOtherSales: number;
  totalRefunds: number;
  status: number;
}

export interface DashboardKpi {
  todaySales: number;
  todayInvoiceCount: number;
  todayProfit: number;
  monthSales: number;
  monthInvoiceCount: number;
  monthProfit: number;
  customerCount: number;
  productCount: number;
  lowStockCount: number;
  openSessionCount: number;
  totalStockValue: number;
}

export interface SalesReportRow {
  date: string;
  invoiceCount: number;
  netSales: number;
  vatAmount: number;
  totalSales: number;
  profit: number;
}

export interface SalesReport {
  from: string;
  to: string;
  rows: SalesReportRow[];
  totalNetSales: number;
  totalVat: number;
  totalGross: number;
  totalProfit: number;
  totalInvoices: number;
}

export interface TopProduct {
  productId: string;
  productName: string;
  quantitySold: number;
  revenue: number;
  profit: number;
}

export interface StockReportRow {
  productId: string;
  productName: string;
  sku: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  averageCost: number;
  stockValue: number;
  minQuantity: number;
  isLow: boolean;
}

export interface StockTransferItem {
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitCost: number;
}

export interface StockTransfer {
  id: string;
  transferNumber: string;
  fromWarehouseId: string;
  fromWarehouseName: string;
  toWarehouseId: string;
  toWarehouseName: string;
  transferDate: string;
  isCompleted: boolean;
  notes?: string | null;
  items: StockTransferItem[];
  totalQuantity: number;
  totalValue: number;
}

export interface CompanyProfile {
  id: string;
  nameAr: string;
  nameEn?: string | null;
  taxRegistrationNumber: string;
  commercialRegister?: string | null;
  activityCode?: string | null;
  address: string;
  governorate?: string | null;
  city?: string | null;
  phone?: string | null;
  email?: string | null;
  etaClientId?: string | null;
  hasEtaSecret: boolean;
  etaIssuerId?: string | null;
  etaEnabled: boolean;
}

export interface EInvoiceSubmission {
  id: string;
  saleId: string;
  submissionUuid?: string | null;
  longId?: string | null;
  hashKey?: string | null;
  status: number;
  errorMessage?: string | null;
  createdAt: string;
  submittedAt?: string | null;
  validatedAt?: string | null;
}

export const SaleStatus: Record<number, string> = {
  0: "مسودة",
  1: "مكتملة",
  2: "ملغاة",
  3: "مرتجعة",
  4: "مرتجعة جزئياً",
};

export const EInvoiceStatusLabels: Record<number, string> = {
  0: "قيد الإرسال",
  1: "تم التقديم",
  2: "مقبولة",
  3: "غير صالحة",
  4: "مرفوضة",
  5: "ملغاة",
};

export const PaymentMethodLabels: Record<number, string> = {
  1: "نقدي",
  2: "بطاقة",
  3: "إنستا باي",
  4: "محفظة",
  5: "تحويل بنكي",
  6: "آجل",
  7: "قسيمة",
};

export interface Category {
  id: string;
  nameAr: string;
  nameEn?: string | null;
  parentCategoryId?: string | null;
  parentName?: string | null;
  isActive: boolean;
}

export interface Unit {
  id: string;
  nameAr: string;
  nameEn?: string | null;
  code: string;
  isActive: boolean;
}

export interface Supplier {
  id: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  taxRegistrationNumber?: string | null;
  commercialRegister?: string | null;
  balance: number;
  isActive: boolean;
}

export interface PurchaseInvoiceItem {
  id: string;
  productId: string;
  productName?: string | null;
  quantity: number;
  unitCost: number;
  discountAmount: number;
  vatRate: number;
  vatAmount: number;
  lineTotal: number;
}

export interface PurchaseInvoice {
  id: string;
  invoiceNumber: string;
  supplierId: string;
  supplierName?: string | null;
  warehouseId: string;
  warehouseName?: string | null;
  invoiceDate: string;
  subTotal: number;
  discountAmount: number;
  vatAmount: number;
  total: number;
  paid: number;
  remaining: number;
  notes?: string | null;
  items: PurchaseInvoiceItem[];
}

export interface ApiUser {
  id: string;
  userName: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  defaultWarehouseId?: string | null;
  defaultCashRegisterId?: string | null;
  isActive: boolean;
  roles: string[];
}
