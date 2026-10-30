import type { GetProductByBarcodeInput, ProductLookupResultDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ProductLookupService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getByBarcode = (input: GetProductByBarcodeInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProductLookupResultDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/product-lookup/by-barcode',
      params: { barcode: input.barcode },
    },
    { apiName: this.apiName,...config });
}