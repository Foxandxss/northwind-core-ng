// tslint:disable:no-trailing-whitespace
// tslint:disable:member-ordering
import { Order } from './order';
import { Product } from './product';

/// <code-import> Place custom imports between <code-import> tags

/// </code-import>

export class OrderItem  {

  /// <code> Place custom code between <code> tags
  
  /// </code>

  // Generated code. Do not place code below this line.
  id: number;
  orderId: number;
  productId: number;
  quantity: number;
  unitPrice: number;
  order: Order;
  product: Product;
}

