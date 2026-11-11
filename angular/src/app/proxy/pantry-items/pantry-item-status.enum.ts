import { mapEnumToOptions } from '@abp/ng.core';

export enum PantryItemStatus {
  Active = 0,
  Consumed = 1,
}

export const pantryItemStatusOptions = mapEnumToOptions(PantryItemStatus);
