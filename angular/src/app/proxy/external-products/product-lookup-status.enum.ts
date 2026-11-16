import { mapEnumToOptions } from '@abp/ng.core';

export enum ProductLookupStatus {
  Found = 1,
  NotFound = 2,
  RateLimited = 3,
  ServiceUnavailable = 4,
}

export const productLookupStatusOptions = mapEnumToOptions(ProductLookupStatus);
