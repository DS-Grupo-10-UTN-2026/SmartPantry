import type { EntityDto } from '@abp/ng.core';

export interface CreateProductDto {
  barcode: string;
  name: string;
}

export interface ProductDto extends EntityDto<string> {
  barcode?: string;
  name?: string;
}

export interface UpdateProductDto {
  name: string;
}
