import type { ProductLookupStatus } from './product-lookup-status.enum';

export interface ExternalProductInfoDto {
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  quantity?: string | null;
  imageUrl?: string | null;
  energyKcalPer100g?: number | null;
  fatPer100g?: number | null;
  sugarsPer100g?: number | null;
  saltPer100g?: number | null;
}

export interface GetProductByBarcodeInput {
  barcode: string;
}

export interface ProductLookupResultDto {
  status?: ProductLookupStatus;
  product?: ExternalProductInfoDto | null;
}
