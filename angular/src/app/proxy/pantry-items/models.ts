import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { PantryItemStatus } from './pantry-item-status.enum';

export interface CreatePantryItemDto {
  productId?: string;
  expirationDate?: string | null;
  quantity?: number | null;
  unit?: string | null;
}

export interface GetPantryItemListInput extends PagedAndSortedResultRequestDto {
  includeConsumed?: boolean;
}

export interface PantryItemDto extends EntityDto<string> {
  productId?: string;
  productName?: string | null;
  status?: PantryItemStatus;
  expirationDate?: string | null;
  quantity?: number | null;
  unit?: string | null;
}

export interface UpdatePantryItemDto {
  expirationDate?: string | null;
  quantity?: number | null;
  unit?: string | null;
}
