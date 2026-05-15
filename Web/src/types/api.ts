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
  imageUrl?: string | null;
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
  todayExpenses: number;
  todayNetProfit: number;
  monthSales: number;
  monthInvoiceCount: number;
  monthProfit: number;
  monthExpenses: number;
  monthNetProfit: number;
  customerCount: number;
  productCount: number;
  lowStockCount: number;
  openSessionCount: number;
  totalStockValue: number;
}

export interface TopCustomer {
  customerId: string;
  customerName: string;
  invoiceCount: number;
  totalSpent: number;
  lastPurchase: string;
}

export interface Expense {
  id: string;
  title: string;
  category: number;
  amount: number;
  expenseDate: string;
  paymentMethod: number;
  reference?: string | null;
  notes?: string | null;
  cashSessionId?: string | null;
  createdAt: string;
}

export interface ExpenseSummary {
  from: string;
  to: string;
  total: number;
  count: number;
  byCategory: { category: number; total: number; count: number }[];
}

export interface ProfitLossReport {
  from: string;
  to: string;
  grossSales: number;
  discounts: number;
  refunds: number;
  netSales: number;
  costOfGoodsSold: number;
  grossProfit: number;
  grossMarginPercent: number;
  operatingExpenses: number;
  expensesByCategory: { category: string; categoryId: number; amount: number; percentOfTotal: number }[];
  netProfit: number;
  netMarginPercent: number;
}

export interface InventoryAgingRow {
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  averageCost: number;
  stockValue: number;
  lastSoldAt?: string | null;
  daysSinceLastSale: number;
  bucket: number; // 0=0-30, 1=30-60, 2=60-90, 3=90-180, 4=180+/never
}

export interface CashierPerformanceRow {
  cashierUserId: string;
  cashierName: string;
  invoiceCount: number;
  totalSales: number;
  averageTicket: number;
  refundCount: number;
  refundsAmount: number;
  netSales: number;
}

export interface CashFlowReport {
  from: string;
  to: string;
  cashSalesIn: number;
  cardSalesIn: number;
  otherSalesIn: number;
  totalIn: number;
  purchasesOut: number;
  expensesOut: number;
  refundsOut: number;
  totalOut: number;
  netCashFlow: number;
  daily: { date: string; in: number; out: number; net: number }[];
}

export interface Coupon {
  id: string;
  code: string;
  description?: string | null;
  type: number;
  value: number;
  minSubtotal: number;
  maxDiscountAmount?: number | null;
  validFrom?: string | null;
  validTo?: string | null;
  maxUses?: number | null;
  maxUsesPerCustomer?: number | null;
  usageCount: number;
  isActive: boolean;
}

export interface LoyaltySettings {
  enabled: boolean;
  pointValueEgp: number;
  egpPerPointEarned: number;
  minRedeemPoints: number;
  maxRedeemPercent: number;
}

export interface LoyaltyTransaction {
  id: string;
  customerId: string;
  type: number;
  points: number;
  balanceAfter: number;
  saleId?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface CustomerLoyaltyStatus {
  customerId: string;
  customerName: string;
  currentPoints: number;
  pointsValue: number;
  recentTransactions: LoyaltyTransaction[];
}

export const LoyaltyTxTypeLabels: Record<number, string> = {
  1: "اكتساب",
  2: "استبدال",
  3: "تعديل يدوي",
  4: "انتهاء صلاحية",
};

export const ExpenseCategoryLabels: Record<number, string> = {
  1: "إيجار",
  2: "رواتب",
  3: "مرافق",
  4: "صيانة",
  5: "تسويق",
  6: "مواصلات",
  7: "مستلزمات",
  8: "ضرائب ورسوم",
  9: "عمولات بنكية",
  99: "أخرى",
};

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

export interface QuotationItem {
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

export interface Quotation {
  id: string;
  quotationNumber: string;
  customerId?: string | null;
  customerName?: string | null;
  customerNameSnapshot?: string | null;
  customerPhoneSnapshot?: string | null;
  warehouseId?: string | null;
  issueDate: string;
  validUntil?: string | null;
  subTotal: number;
  discountAmount: number;
  discountPercent: number;
  vatAmount: number;
  total: number;
  status: number;
  convertedSaleId?: string | null;
  notes?: string | null;
  terms?: string | null;
  createdAt: string;
  items: QuotationItem[];
}

export const QuotationStatusLabels: Record<number, string> = {
  0: "مسودة",
  1: "مُرسل",
  2: "مقبول",
  3: "مرفوض",
  4: "منتهي",
  5: "محوّل",
  6: "ملغي",
};

export const QuotationStatusColors: Record<number, string> = {
  0: "bg-slate-100 text-slate-800",
  1: "bg-blue-100 text-blue-800",
  2: "bg-emerald-100 text-emerald-800",
  3: "bg-red-100 text-red-800",
  4: "bg-amber-100 text-amber-800",
  5: "bg-violet-100 text-violet-800",
  6: "bg-slate-200 text-slate-600",
};

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

export interface StockMovement {
  id: string;
  productId: string;
  productName?: string | null;
  warehouseId: string;
  warehouseName?: string | null;
  type: number;
  quantity: number;
  unitCost: number;
  balanceAfter: number;
  documentNumber?: string | null;
  notes?: string | null;
  movementDate: string;
}

export const MovementTypeLabels: Record<number, string> = {
  1: "شراء",
  2: "بيع",
  3: "تحويل وارد",
  4: "تحويل صادر",
  5: "تسوية وارد",
  6: "تسوية صادر",
  7: "مرتجع وارد",
  8: "مرتجع صادر",
  9: "رصيد افتتاحي",
};

export interface LogHistory {
  id: number;
  entityName: string;
  entityId: number;
  action: string;
  oldValues?: string | null;
  newValues?: string | null;
  changedFields?: string | null;
  userId: string;
  userName: string;
  timestamp: string;
  notes?: string | null;
}

// ─── HR ─────────────────────────────────────────────────────────────────

export interface Position {
  id: string;
  title: string;
  baseSalary: number;
  departmentId?: string | null;
  departmentName?: string | null;
  description?: string | null;
  isActive: boolean;
  employeeCount: number;
}

export interface HrEmployee {
  id: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  nationalId?: string | null;
  address?: string | null;
  photoUrl?: string | null;
  hireDate: string;
  terminationDate?: string | null;
  status: number;
  departmentId: string;
  departmentName?: string | null;
  positionId?: string | null;
  positionTitle?: string | null;
  baseSalary: number;
  allowances: number;
  deductions: number;
  overtimeHourlyRate: number;
  bankName?: string | null;
  bankAccount?: string | null;
  notes?: string | null;
}

export const EmpStatusLabel: Record<number, string> = {
  0: "نشط",
  1: "موقوف",
  2: "غير نشط",
};

export interface Shift {
  id: string;
  name: string;
  startTime: string;
  endTime: string;
  daysMask: number;
  graceMinutes: number;
  standardHours: number;
  overtimeMultiplier: number;
  latePenaltyPerMinute: number;
  isActive: boolean;
}

export interface ShiftAssignment {
  id: string;
  employeeId: string;
  employeeName?: string | null;
  shiftId: string;
  shiftName?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  notes?: string | null;
}

export interface Attendance {
  id: string;
  employeeId: string;
  employeeName?: string | null;
  date: string;
  checkIn?: string | null;
  checkOut?: string | null;
  shiftId?: string | null;
  shiftName?: string | null;
  workedHours: number;
  overtimeHours: number;
  lateMinutes: number;
  earlyLeaveMinutes: number;
  status: number;
  notes?: string | null;
}

export const AttendanceStatusLabel: Record<number, string> = {
  0: "حاضر",
  1: "غائب",
  2: "متأخر",
  3: "إجازة",
  4: "عطلة",
  5: "ويك إند",
};

export interface AttendanceSummary {
  employeeId: string;
  employeeName: string;
  presentDays: number;
  absentDays: number;
  lateDays: number;
  totalWorkedHours: number;
  totalOvertimeHours: number;
  totalLateMinutes: number;
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName?: string | null;
  type: number;
  from: string;
  to: string;
  days: number;
  status: number;
  reason?: string | null;
  createdAt: string;
  approvedAt?: string | null;
}

export const LeaveTypeLabel: Record<number, string> = {
  0: "سنوية",
  1: "مرضية",
  2: "عارضة",
  3: "بدون أجر",
  4: "وضع",
  5: "طارئة",
  6: "أخرى",
};

export const LeaveStatusLabel: Record<number, string> = {
  0: "قيد المراجعة",
  1: "موافق عليها",
  2: "مرفوضة",
  3: "ملغاة",
};

export interface Payroll {
  id: string;
  employeeId: string;
  employeeName?: string | null;
  year: number;
  month: number;
  baseSalary: number;
  allowances: number;
  deductions: number;
  overtimePay: number;
  latePenalty: number;
  unpaidLeavePenalty: number;
  bonus: number;
  tax: number;
  insuranceContribution: number;
  workingDays: number;
  absentDays: number;
  overtimeHours: number;
  lateMinutes: number;
  grossPay: number;
  netPay: number;
  status: number;
  notes?: string | null;
  approvedAt?: string | null;
  paidAt?: string | null;
}

export const PayrollStatusLabel: Record<number, string> = {
  0: "مسودة",
  1: "معتمدة",
  2: "مدفوعة",
  3: "ملغاة",
};
